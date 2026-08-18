using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public record GetPrintableInvoiceQuery(long Id) : IRequest<PrintableInvoiceDto?>;

public class PrintableInvoiceDto
{
    public long InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public string EmployeeName { get; init; } = string.Empty;
    public DateTime Date { get; init; }
    public string PaymentMethod { get; init; } = "Cash";
    public string PaymentStatus { get; init; } = "Paid";
    public string? CustomerNickname { get; init; }
    public decimal? OutstandingBalance { get; init; }
    public decimal TotalBeforeDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TotalAfterDiscount { get; init; }
    public bool HasReturn { get; init; }
    public List<PrintableInvoiceItemDto> Items { get; init; } = new();
    public string HtmlReceipt { get; init; } = string.Empty;
}

public class PrintableInvoiceItemDto
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}
