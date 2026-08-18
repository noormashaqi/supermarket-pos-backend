using Dapper;
using MediatR;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Create;

public class CreateInvoiceHandler : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CreateInvoiceHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1) قفل المنتجات المطلوبة وجلب السعر/الكمية/الحالة الحالية
            var groupedItems = request.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new CreateInvoiceItemDto(
                    g.Key,
                    g.Sum(x => x.Quantity),
                    g.Select(x => x.UnitPrice).FirstOrDefault(p => p.HasValue && p.Value > 0)
                ))
                .ToList();

            var productIds = groupedItems.Select(i => i.ProductId).ToList();

            var products = (await connection.QueryAsync<ProductStockDto>(
                new CommandDefinition(
                    "SELECT Id, Name, SellingPrice, Quantity, IsActive FROM Product WHERE Id IN @Ids FOR UPDATE",
                    new { Ids = productIds },
                    transaction: transaction,
                    cancellationToken: cancellationToken)
            )).ToDictionary(p => p.Id);

            // 2) التحقق: المنتج موجود، مفعّل، كميته كافية، والتحقق من صلاحية تعديل السعر (Price Override)
            bool? employeeHasOverridePermission = null;

            foreach (var item in groupedItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    throw new InvalidOperationException($"Product {item.ProductId} not found.");

                if (!product.IsActive)
                    throw new InvalidOperationException($"Product {item.ProductId} is deactivated and cannot be sold.");

                if (item.Quantity > product.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product {item.ProductId}. Available: {product.Quantity}, requested: {item.Quantity}.");

                var effectivePrice = item.UnitPrice ?? product.SellingPrice;
                if (effectivePrice != product.SellingPrice)
                {
                    if (!employeeHasOverridePermission.HasValue)
                    {
                        var employeeRole = await connection.ExecuteScalarAsync<string>(
                            new CommandDefinition(
                                "SELECT Role FROM Employees WHERE Id = @EmployeeId",
                                new { request.EmployeeId },
                                transaction: transaction,
                                cancellationToken: cancellationToken));

                        if (string.Equals(employeeRole, "Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            employeeHasOverridePermission = true;
                        }
                        else
                        {
                            var permCount = await connection.ExecuteScalarAsync<int>(
                                new CommandDefinition(
                                    "SELECT COUNT(1) FROM EmployeePermissions WHERE EmployeeId = @EmployeeId AND PermissionKey = @PermKey",
                                    new { request.EmployeeId, PermKey = PermissionKeys.InvoicesOverridePrice },
                                    transaction: transaction,
                                    cancellationToken: cancellationToken));

                            employeeHasOverridePermission = permCount > 0;
                        }
                    }

                    if (!employeeHasOverridePermission.Value)
                    {
                        throw new UnauthorizedAccessException(
                            $"Employee {request.EmployeeId} does not have permission to override product price for Product {item.ProductId} ({product.Name}). Base price: {product.SellingPrice}, requested: {effectivePrice}.");
                    }
                }
            }

            // 3) توليد InvoiceNumber (تاريخ اليوم + رقم متسلسل يصفر يوميًا)
            var today = DateTime.UtcNow.Date;
            var todayPrefix = today.ToString("yyyyMMdd");

            var lastSequence = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    @"SELECT MAX(CAST(SUBSTRING_INDEX(InvoiceNumber, '-', -1) AS UNSIGNED))
                      FROM Invoices
                      WHERE Date >= @Today AND Date < @Tomorrow
                      FOR UPDATE",
                    new { Today = today, Tomorrow = today.AddDays(1) },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var nextSequence = (lastSequence ?? 0) + 1;
            var invoiceNumber = $"{todayPrefix}-{nextSequence:D3}";

            // 4) حساب الإجماليات بالسعر الفعلي (سواء السعر الأساسي أو السعر المعدل)
            decimal totalBeforeDiscount = 0;
            var lineItems = new List<(int ProductId, string Name, decimal Price, int Quantity, decimal LineTotal)>();

            foreach (var item in groupedItems)
            {
                var product = products[item.ProductId];
                var effectivePrice = item.UnitPrice ?? product.SellingPrice;
                var lineTotal = effectivePrice * item.Quantity;
                totalBeforeDiscount += lineTotal;
                lineItems.Add((item.ProductId, product.Name, effectivePrice, item.Quantity, lineTotal));
            }

            var totalAfterDiscount = totalBeforeDiscount * (1 - request.DiscountPercentage / 100m);

            // 5) إدخال الفاتورة
            var invoiceId = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    @"INSERT INTO Invoices
                        (InvoiceNumber, EmployeeId, Date, TotalBeforeDiscount, DiscountPercentage, TotalAfterDiscount, HasReturn)
                      VALUES
                        (@InvoiceNumber, @EmployeeId, @Date, @TotalBeforeDiscount, @DiscountPercentage, @TotalAfterDiscount, FALSE);
                      SELECT LAST_INSERT_ID();",
                    new
                    {
                        InvoiceNumber = invoiceNumber,
                        request.EmployeeId,
                        Date = DateTime.UtcNow,
                        TotalBeforeDiscount = totalBeforeDiscount,
                        request.DiscountPercentage,
                        TotalAfterDiscount = totalAfterDiscount
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // 6) إدخال InvoiceItems + إنقاص المخزون
            foreach (var line in lineItems)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"INSERT INTO InvoiceItems
                            (InvoiceId, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal)
                          VALUES
                            (@InvoiceId, @ProductId, @ProductNameSnapshot, @UnitPriceSnapshot, @Quantity, @LineTotal)",
                        new
                        {
                            InvoiceId = invoiceId,
                            line.ProductId,
                            ProductNameSnapshot = line.Name,
                            UnitPriceSnapshot = line.Price,
                            line.Quantity,
                            line.LineTotal
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "UPDATE Product SET Quantity = Quantity - @Qty WHERE Id = @ProductId",
                        new { Qty = line.Quantity, line.ProductId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            transaction.Commit();

            return new CreateInvoiceResult(invoiceId, invoiceNumber);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

file record ProductStockDto(int Id, string Name, decimal SellingPrice, int Quantity, bool IsActive);
