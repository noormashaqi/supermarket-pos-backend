namespace SupermarketSystem.Api.Features.Invoices.Read;

public class InvoiceItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductNameSnapshot { get; init; } = string.Empty;
    public decimal UnitPriceSnapshot { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public class InvoiceDto
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
    public List<InvoiceItemDto> Items { get; init; } = new();
}