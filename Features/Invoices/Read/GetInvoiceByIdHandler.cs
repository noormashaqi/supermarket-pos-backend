using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public class GetInvoiceByIdHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDto?>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetInvoiceByIdHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<InvoiceDto?> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var invoiceRows = (await connection.QueryAsync<InvoiceHeaderRow>(
            new CommandDefinition(
                @"SELECT i.Id, i.InvoiceNumber, i.EmployeeId, COALESCE(e.FullName, 'Staff') AS EmployeeName,
                         i.Date, i.TotalBeforeDiscount, i.DiscountPercentage, i.TotalAfterDiscount, i.HasReturn,
                         i.PaymentMethod, i.PaymentStatus, i.CustomerId,
                         COALESCE(c.Nickname, c.FullName) AS CustomerName
                  FROM Invoices i
                  LEFT JOIN Employees e ON e.Id = i.EmployeeId
                  LEFT JOIN Customers c ON c.Id = i.CustomerId
                  WHERE i.Id = @Id",
                new { request.Id },
                cancellationToken: cancellationToken))).ToList();

        var header = invoiceRows.FirstOrDefault();
        if (header is null)
            return null;

        var items = (await connection.QueryAsync<InvoiceItemDto>(
            new CommandDefinition(
                @"SELECT Id, ProductId, ProductNameSnapshot, UnitPriceSnapshot, Quantity, LineTotal
                  FROM InvoiceItems
                  WHERE InvoiceId = @Id",
                new { request.Id },
                cancellationToken: cancellationToken))).ToList();

        return new InvoiceDto
        {
            Id = header.Id,
            InvoiceNumber = header.InvoiceNumber,
            EmployeeId = header.EmployeeId,
            EmployeeName = header.EmployeeName,
            Date = header.Date,
            TotalBeforeDiscount = header.TotalBeforeDiscount,
            DiscountPercentage = header.DiscountPercentage,
            TotalAfterDiscount = header.TotalAfterDiscount,
            HasReturn = header.HasReturn,
            PaymentMethod = header.PaymentMethod,
            PaymentStatus = header.PaymentStatus,
            CustomerId = header.CustomerId,
            CustomerName = header.CustomerName,
            Items = items
        };
    }
}

file class InvoiceHeaderRow
{
    public long Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public long EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public decimal TotalBeforeDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal TotalAfterDiscount { get; init; }
    public bool HasReturn { get; init; }
    public string PaymentMethod { get; init; } = "Cash";
    public string PaymentStatus { get; init; } = "Paid";
    public long? CustomerId { get; init; }
    public string? CustomerName { get; init; }
}