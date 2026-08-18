using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Customers.Payment;

public class RecordPaymentHandler : IRequestHandler<RecordPaymentCommand, RecordPaymentResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RecordPaymentHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RecordPaymentResult> Handle(RecordPaymentCommand request, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1) Lock the customer row and fetch current balance
            var customer = await connection.QuerySingleOrDefaultAsync<CustomerBalanceDto>(
                new CommandDefinition(
                    "SELECT Id, CurrentBalance FROM Customers WHERE Id = @CustomerId FOR UPDATE",
                    new { request.CustomerId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (customer is null)
                throw new KeyNotFoundException($"Customer with id {request.CustomerId} not found.");

            if (request.Amount > customer.CurrentBalance)
                throw new InvalidOperationException(
                    $"Payment amount ({request.Amount}) exceeds the customer's outstanding balance ({customer.CurrentBalance}).");

            // 2) Insert payment record
            var paymentId = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    @"INSERT INTO CustomerPayments (CustomerId, Amount, EmployeeId, Notes)
                      VALUES (@CustomerId, @Amount, @EmployeeId, @Notes);
                      SELECT LAST_INSERT_ID();",
                    new
                    {
                        request.CustomerId,
                        request.Amount,
                        request.EmployeeId,
                        request.Notes
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            // 3) Decrement the customer's balance
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "UPDATE Customers SET CurrentBalance = CurrentBalance - @Amount WHERE Id = @CustomerId",
                    new { request.Amount, request.CustomerId },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            var newBalance = customer.CurrentBalance - request.Amount;

            transaction.Commit();

            return new RecordPaymentResult(paymentId, newBalance);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}

file record CustomerBalanceDto(long Id, decimal CurrentBalance);
