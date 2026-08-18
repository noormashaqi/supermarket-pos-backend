using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Customers.Create;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CreateCustomerHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CreateCustomerResult> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var id = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                @"INSERT INTO Customers (FullName, Nickname, PhoneNumber)
                  VALUES (@FullName, @Nickname, @PhoneNumber);
                  SELECT LAST_INSERT_ID();",
                new
                {
                    request.FullName,
                    request.Nickname,
                    request.PhoneNumber
                },
                cancellationToken: cancellationToken));

        return new CreateCustomerResult(id);
    }
}
