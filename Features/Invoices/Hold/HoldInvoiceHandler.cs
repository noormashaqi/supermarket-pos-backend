using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public class HoldInvoiceHandler : IRequestHandler<HoldInvoiceCommand, HoldInvoiceResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HoldInvoiceHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HoldInvoiceResult> Handle(HoldInvoiceCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        const string sql = """
            INSERT INTO HeldInvoices 
                (EmployeeId, ReferenceTag, CustomerName, DiscountPercentage, CartState)
            VALUES 
                (@EmployeeId, @ReferenceTag, @CustomerName, @DiscountPercentage, @CartState);
            SELECT LAST_INSERT_ID();
            """;

        var id = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    request.EmployeeId,
                    request.ReferenceTag,
                    request.CustomerName,
                    request.DiscountPercentage,
                    request.CartState
                },
                cancellationToken: cancellationToken
            )
        );

        return new HoldInvoiceResult(id, request.ReferenceTag);
    }
}
