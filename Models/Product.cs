namespace SupermarketSystem.Api.Models;

public enum ProductUnit
{
    Piece,
    Package
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public ProductUnit Unit { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}