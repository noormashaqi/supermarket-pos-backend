using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Customers.Read;

public class GetCustomersHandler : IRequestHandler<GetCustomersQuery, List<CustomerDto>>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetCustomersHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var customers = await connection.QueryAsync<CustomerDto>(
            new CommandDefinition(
                @"SELECT Id, FullName, Nickname, PhoneNumber, CurrentBalance, CreatedAt
                  FROM Customers
                  ORDER BY FullName",
                cancellationToken: cancellationToken));

        return customers.ToList();
    }
}
