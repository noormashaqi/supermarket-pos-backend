using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public class InvoiceListItemDto
{
    public int Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public int EmployeeId { get; init; }
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public decimal TotalAfterDiscount { get; init; }
    public bool HasReturn { get; init; }
    public string PaymentMethod { get; init; } = "Cash";
    public string PaymentStatus { get; init; } = "Paid";
}

public record GetInvoicesQuery(
    DateTime? Date,
    int? EmployeeId,
    int? ProductId
) : IRequest<List<InvoiceListItemDto>>;