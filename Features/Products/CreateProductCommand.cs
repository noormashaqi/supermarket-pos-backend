using MediatR;
using SupermarketSystem.Api.DTOs;

namespace SupermarketSystem.Api.Services.Products;

public class CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal CostPrice { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public long EmployeeId { get; set; }
}