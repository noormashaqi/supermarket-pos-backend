namespace SupermarketSystem.Api.Models;

public class Customer
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? PhoneNumber { get; set; }
    public decimal CurrentBalance { get; set; }
    public DateTime CreatedAt { get; set; }
}
