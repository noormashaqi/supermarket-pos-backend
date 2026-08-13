using Dapper;
using MediatR;
using SupermarketSystem.Api.Interface;

namespace SupermarketSystem.Api.Features.Employees;

public record SetEmployeeStatusCommand(long Id, bool IsActive, long? ActorEmployeeId)
    : IRequest<SetEmployeeStatusResult>;

public class SetEmployeeStatusResult
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public static SetEmployeeStatusResult Succeeded(bool isActive)
    {
        return new SetEmployeeStatusResult
        {
            Success = true,
            Message = isActive
                ? "Employee activated successfully."
                : "Employee deactivated successfully."
        };
    }

    public static SetEmployeeStatusResult Failed(string errorCode, string message)
    {
        return new SetEmployeeStatusResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

public class SetEmployeeStatusHandler
    : IRequestHandler<SetEmployeeStatusCommand, SetEmployeeStatusResult>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public SetEmployeeStatusHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SetEmployeeStatusResult> Handle(
        SetEmployeeStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            return SetEmployeeStatusResult.Failed(
                "ValidationError",
                "Employee id must be greater than zero.");
        }

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        const string selectSql = """
            SELECT Id, IsActive
            FROM Employees
            WHERE Id = @Id
            LIMIT 1
            FOR UPDATE;
            """;

        var employee = await connection.QuerySingleOrDefaultAsync<EmployeeStatusRow>(
            new CommandDefinition(
                selectSql,
                new { request.Id },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (employee is null)
        {
            transaction.Rollback();
            return SetEmployeeStatusResult.Failed("NotFound", "Employee not found.");
        }

        if (request.ActorEmployeeId == request.Id && !request.IsActive)
        {
            transaction.Rollback();
            return SetEmployeeStatusResult.Failed(
                "SelfDeactivationNotAllowed",
                "You cannot deactivate your own account.");
        }

        if (employee.IsActive == request.IsActive)
        {
            transaction.Rollback();
            return SetEmployeeStatusResult.Failed(
                request.IsActive ? "AlreadyActive" : "AlreadyDeactivated",
                request.IsActive
                    ? "Employee is already active."
                    : "Employee is already deactivated.");
        }

        const string updateEmployeeSql = """
            UPDATE Employees
            SET IsActive = @IsActive
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                updateEmployeeSql,
                new
                {
                    request.Id,
                    request.IsActive
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

        if (!request.IsActive)
        {
            const string revokeTokensSql = """
                UPDATE RefreshTokens
                SET RevokedAt = UTC_TIMESTAMP()
                WHERE EmployeeId = @Id
                  AND RevokedAt IS NULL;
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    revokeTokensSql,
                    new { request.Id },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }

        transaction.Commit();

        return SetEmployeeStatusResult.Succeeded(request.IsActive);
    }

    private sealed class EmployeeStatusRow
    {
        public long Id { get; set; }
        public bool IsActive { get; set; }
    }
}
