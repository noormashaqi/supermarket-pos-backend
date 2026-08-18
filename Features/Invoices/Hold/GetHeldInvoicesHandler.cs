using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public class GetHeldInvoicesHandler : IRequestHandler<GetHeldInvoicesQuery, List<HeldInvoiceDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetHeldInvoicesHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<HeldInvoiceDto>> Handle(GetHeldInvoicesQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        const string sql = """
            SELECT Id, ReferenceTag, CustomerName, DiscountPercentage, CartState, CreatedAt
            FROM HeldInvoices
            WHERE EmployeeId = @EmployeeId
            ORDER BY CreatedAt DESC;
            """;

        var result = await connection.QueryAsync<HeldInvoiceDto>(
            new CommandDefinition(
                sql,
                new { request.EmployeeId },
                cancellationToken: cancellationToken
            )
        );

        return result.ToList();
    }
}
