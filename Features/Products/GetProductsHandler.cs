using Dapper;
using MediatR;
using SupermarketSystem.Api.DTOs;
using SupermarketSystem.Api.Interface;
using System.Text;

namespace SupermarketSystem.Api.Services.Products;

public class GetProductsHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IDbConnectionFactory _dbFactory;

    public GetProductsHandler(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbFactory.CreateConnection();

        var sql = new StringBuilder(@"
            SELECT p.Id, p.Name, p.CategoryId, c.Name AS CategoryName,
                   p.SellingPrice, p.CostPrice, p.Quantity, p.Unit, p.IsActive, p.CreatedAt
            FROM Product p
            INNER JOIN Category c ON c.Id = p.CategoryId
            WHERE 1 = 1");

        var parameters = new DynamicParameters();

        if (request.CategoryId.HasValue)
        {
            sql.Append(" AND p.CategoryId = @CategoryId");
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        if (request.ActiveOnly)
        {
            sql.Append(" AND p.IsActive = 1");
        }

        sql.Append(" ORDER BY p.Name");

        var products = await connection.QueryAsync<ProductDto>(sql.ToString(), parameters);

        return products.ToList();
    }
}