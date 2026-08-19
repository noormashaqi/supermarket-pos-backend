using MediatR;
using SupermarketSystem.Api.DTOs;

namespace SupermarketSystem.Api.Services.Products;

public class UpdateProductCommand : IRequest<ProductDto>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string Unit { get; set; } = string.Empty;

    // Note: Quantity is intentionally NOT included here.
    // Quantity can only change via stock-add or invoice confirmation.
}