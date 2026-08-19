using Dapper;
using MediatR;
using SupermarketSystem.Api.DTOs;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Services.Products;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IDbConnectionFactory _dbFactory;

    public GetProductByIdHandler(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbFactory.CreateConnection();

        var product = await connection.QuerySingleOrDefaultAsync<ProductDto>(@"
            SELECT p.Id, p.Name, p.CategoryId, c.Name AS CategoryName,
                   p.SellingPrice, p.CostPrice, p.Quantity, p.Unit, p.IsActive, p.CreatedAt
            FROM Product p
            INNER JOIN Category c ON c.Id = p.CategoryId
            WHERE p.Id = @Id",
            new { request.Id });

        return product;
    }
}