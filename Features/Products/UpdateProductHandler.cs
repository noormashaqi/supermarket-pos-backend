using Dapper;
using MediatR;
using SupermarketSystem.Api.DTOs;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Services.Products;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IDbConnectionFactory _dbFactory;

    public UpdateProductHandler(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        using var connection = _dbFactory.CreateConnection();

        // Quantity is deliberately excluded from this UPDATE statement.
        await connection.ExecuteAsync(@"
            UPDATE Product
            SET Name = @Name,
                CategoryId = @CategoryId,
                SellingPrice = @SellingPrice,
                CostPrice = @CostPrice,
                Unit = @Unit
            WHERE Id = @Id",
            new
            {
                request.Id,
                request.Name,
                request.CategoryId,
                request.SellingPrice,
                request.CostPrice,
                request.Unit
            });

        var updated = await connection.QuerySingleAsync<ProductDto>(@"
            SELECT p.Id, p.Name, p.CategoryId, c.Name AS CategoryName,
                   p.SellingPrice, p.CostPrice, p.Quantity, p.Unit, p.IsActive, p.CreatedAt
            FROM Product p
            INNER JOIN Category c ON c.Id = p.CategoryId
            WHERE p.Id = @Id",
            new { request.Id });

        return updated;
    }
}