public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public DateTime Date { get; set; }
    public decimal TotalBeforeDiscount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal TotalAfterDiscount { get; set; }
    public bool HasReturn { get; set; }
    public long? CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
}