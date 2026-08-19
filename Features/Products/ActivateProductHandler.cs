using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Services.Products;

public class ActivateProductHandler : IRequestHandler<ActivateProductCommand, Unit>
{
    private readonly IDbConnectionFactory _dbFactory;

    public ActivateProductHandler(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Unit> Handle(ActivateProductCommand request, CancellationToken cancellationToken)
    {
        using var connection = _dbFactory.CreateConnection();

        await connection.ExecuteAsync(
            "UPDATE Product SET IsActive = 1 WHERE Id = @Id",
            new { request.Id });

        return Unit.Value;
    }
}
