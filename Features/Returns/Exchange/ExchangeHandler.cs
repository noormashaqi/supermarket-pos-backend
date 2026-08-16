using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Returns.Exchange;

public class ExchangeHandler : IRequestHandler<ExchangeCommand, ExchangeResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ExchangeHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ExchangeResult> Handle(ExchangeCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1) الفحص التراكمي على الصنف القديم (نفس منطق Pure Return)
            var originalQuantity = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    @"SELECT Quantity FROM InvoiceItems
                      WHERE InvoiceId = @InvoiceId AND ProductId = @ProductId
                      FOR UPDATE",
                    new { InvoiceId = request.OriginalInvoiceId, ProductId = request.OldProductId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (originalQuantity is null)
                throw new InvalidOperationException(
                    $"Product {request.OldProductId} was not sold on invoice {request.OriginalInvoiceId}.");

            var alreadyReturned = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    @"SELECT SUM(QuantityReturned) FROM returns
                      WHERE OriginalInvoiceId = @InvoiceId AND ProductId = @ProductId
                      FOR UPDATE",
                    new { InvoiceId = request.OriginalInvoiceId, ProductId = request.OldProductId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)) ?? 0;

            if (alreadyReturned + request.QuantityReturned > originalQuantity)
                throw new InvalidOperationException(
                    $"Return quantity exceeds original sold quantity. Sold: {originalQuantity}, already returned: {alreadyReturned}, requested: {request.QuantityReturned}.");

            // 2) التحقق من وجود قائمة العناصر الجديدة وتوفر المخزون وحساب إجمالي الفاتورة الجديدة
            if (request.NewItems is null || !request.NewItems.Any())
                throw new InvalidOperationException("Exchange request must contain at least one new item.");

            decimal totalInvoiceAmount = 0;
            var validatedNewItems = new List<(NewProductStockDto Product, int Quantity, decimal LineTotal)>();

            foreach (var newItem in request.NewItems)
            {
                var newProduct = await connection.QuerySingleOrDefaultAsync<NewProductStockDto>(
                    new CommandDefinition(
                        "SELECT Id, Name, SellingPrice, Quantity, IsActive FROM product WHERE Id = @Id FOR UPDATE",
                        new { Id = newItem.ProductId },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                if (newProduct is null)
                    throw new InvalidOperationException($"Product {newItem.ProductId} not found.");

                if (!newProduct.IsActive)
                    throw new InvalidOperationException($"Product '{newProduct.Name}' (ID: {newItem.ProductId}) is deactivated and cannot be sold.");

                if (newItem.Quantity > newProduct.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{newProduct.Name}'. Available: {newProduct.Quantity}, requested: {newItem.Quantity}.");

                var lineTotal = newProduct.SellingPrice * newItem.Quantity;
                totalInvoiceAmount += lineTotal;
                validatedNewItems.Add((newProduct, newItem.Quantity, lineTotal));
            }

            // 3) إرجاع الصنف القديم للمخزون
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE product SET Quantity = Quantity + @Qty WHERE Id = @ProductId",
                    new { Qty = request.QuantityReturned, ProductId = request.OldProductId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // 4) توليد InvoiceNumber للفاتورة الجديدة
            var today = DateTime.UtcNow.Date;
            var todayPrefix = today.ToString("yyyyMMdd");

            var lastSequence = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    @"SELECT MAX(CAST(SUBSTRING_INDEX(InvoiceNumber, '-', -1) AS UNSIGNED))
                      FROM invoices
                      WHERE Date >= @Today AND Date < @Tomorrow
                      FOR UPDATE",
                    new { Today = today, Tomorrow = today.AddDays(1) },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var nextSequence = (lastSequence ?? 0) + 1;
            var newInvoiceNumber = $"{todayPrefix}-{nextSequence:D3}";

            // 5) إنشاء الفاتورة الجديدة بإجمالي المبالغ للعناصر الجديدة
            var newInvoiceId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    @"INSERT INTO invoices
                        (InvoiceNumber, EmployeeId, Date, TotalBeforeDiscount, DiscountPercentage, TotalAfterDiscount, HasReturn)
                      VALUES
                        (@InvoiceNumber, @EmployeeId, @Date, @Total, 0, @Total, FALSE);
                      SELECT LAST_INSERT_ID();",
                    new
                    {
                        InvoiceNumber = newInvoiceNumber,
                        request.EmployeeId,
                        Date = DateTime.UtcNow,
                        Total = totalInvoiceAmount
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // 6) إدخال الأصناف الجديدة في InvoiceItems وتخصيم كمياتها من الجدول الرئيسي للمنتجات
            foreach (var item in validatedNewItems)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"INSERT INTO InvoiceItems
                            (InvoiceId, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal)
                          VALUES
                            (@InvoiceId, @ProductId, @ProductNameSnapshot, @UnitPriceSnapshot, @Quantity, @LineTotal)",
                        new
                        {
                            InvoiceId = newInvoiceId,
                            ProductId = item.Product.Id,
                            ProductNameSnapshot = item.Product.Name,
                            UnitPriceSnapshot = item.Product.SellingPrice,
                            Quantity = item.Quantity,
                            LineTotal = item.LineTotal
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "UPDATE product SET Quantity = Quantity - @Qty WHERE Id = @ProductId",
                        new { Qty = item.Quantity, ProductId = item.Product.Id },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }

            // 7) إدخال سجل الإرجاع (Type = Exchange، مربوط بالفاتورة الجديدة)
            var returnId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    @"INSERT INTO returns
                        (OriginalInvoiceId, Type, ProductId, QuantityReturned, NewInvoiceId, EmployeeId, Date, Reason)
                      VALUES
                        (@OriginalInvoiceId, 'Exchange', @ProductId, @QuantityReturned, @NewInvoiceId, @EmployeeId, @Date, @Reason);
                      SELECT LAST_INSERT_ID();",
                    new
                    {
                        request.OriginalInvoiceId,
                        ProductId = request.OldProductId,
                        request.QuantityReturned,
                        NewInvoiceId = newInvoiceId,
                        request.EmployeeId,
                        Date = DateTime.UtcNow,
                        request.Reason
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // 8) تعليم الفاتورة الأصلية (HasReturn = 1)
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE invoices SET HasReturn = 1 WHERE Id = @InvoiceId",
                    new { InvoiceId = request.OriginalInvoiceId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            transaction.Commit();

            return new ExchangeResult(returnId, newInvoiceId, newInvoiceNumber);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

file record NewProductStockDto(int Id, string Name, decimal SellingPrice, int Quantity, bool IsActive);