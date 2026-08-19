using Dapper;
using MediatR;
using SupermarketSystem.Api.DTOs;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Services.Products;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IDbConnectionFactory _dbFactory;

    public CreateProductHandler(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        using var connection = _dbFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var newId = await connection.QuerySingleAsync<int>(@"
                INSERT INTO Product (Name, CategoryId, SellingPrice, CostPrice, Quantity, Unit, IsActive, CreatedAt)
                VALUES (@Name, @CategoryId, @SellingPrice, @CostPrice, @Quantity, @Unit, 1, UTC_TIMESTAMP());
                SELECT LAST_INSERT_ID();",
                new
                {
                    request.Name,
                    request.CategoryId,
                    request.SellingPrice,
                    request.CostPrice,
                    request.Quantity,
                    request.Unit
                },
                transaction);

            if (request.Quantity > 0)
            {
                await connection.ExecuteAsync(@"
                    INSERT INTO StockHistory (ProductId, QuantityAdded, EmployeeId, Date)
                    VALUES (@ProductId, @QuantityAdded, @EmployeeId, UTC_TIMESTAMP())",
                    new
                    {
                        ProductId = newId,
                        QuantityAdded = request.Quantity,
                        request.EmployeeId
                    },
                    transaction);
            }

            transaction.Commit();

            var categoryName = await connection.QuerySingleAsync<string>(
                "SELECT Name FROM Category WHERE Id = @CategoryId",
                new { request.CategoryId });

            return new ProductDto
            {
                Id = newId,
                Name = request.Name,
                CategoryId = request.CategoryId,
                CategoryName = categoryName,
                SellingPrice = request.SellingPrice,
                CostPrice = request.CostPrice,
                Quantity = request.Quantity,
                Unit = request.Unit,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}