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

        var fullName = !string.IsNullOrWhiteSpace(request.FullName)
            ? request.FullName
            : (!string.IsNullOrWhiteSpace(request.Name)
                ? request.Name
                : (!string.IsNullOrWhiteSpace(request.Nickname) ? request.Nickname : "Customer"));

        var nickname = !string.IsNullOrWhiteSpace(request.Nickname)
            ? request.Nickname
            : fullName;

        var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? request.PhoneNumber
            : request.Phone;

        var id = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                @"INSERT INTO Customers (FullName, Nickname, PhoneNumber)
                  VALUES (@FullName, @Nickname, @PhoneNumber);
                  SELECT LAST_INSERT_ID();",
                new
                {
                    FullName = fullName,
                    Nickname = nickname,
                    PhoneNumber = phone
                },
                cancellationToken: cancellationToken));

        return new CreateCustomerResult(id, id, fullName, nickname, phone);
    }
}
