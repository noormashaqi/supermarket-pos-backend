namespace SupermarketSystem.Api.Models;

public class CustomerPayment
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public decimal Amount { get; set; }
    public long EmployeeId { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Notes { get; set; }
}
