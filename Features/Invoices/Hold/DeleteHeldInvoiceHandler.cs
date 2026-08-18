using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public class DeleteHeldInvoiceHandler : IRequestHandler<DeleteHeldInvoiceCommand, bool>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DeleteHeldInvoiceHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> Handle(DeleteHeldInvoiceCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        const string sql = """
            DELETE FROM HeldInvoices
            WHERE Id = @Id AND EmployeeId = @EmployeeId;
            """;

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { request.Id, request.EmployeeId },
                cancellationToken: cancellationToken
            )
        );

        return rowsAffected > 0;
    }
}
