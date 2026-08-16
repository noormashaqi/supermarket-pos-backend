using Dapper;
using MediatR;
using MySqlConnector;
using SupermarketSystem.Api.Constants;
using SupermarketSystem.Api.Interface;
using SupermarketSystem.Api.Models;

namespace SupermarketSystem.Api.Features.Employees;

public class CreateEmployeeCommand : IRequest<CreateEmployeeResult>
{
    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public List<string>? Permissions { get; set; }
}

public class CreateEmployeeResult
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string? Message { get; set; }

    public EmployeeResponse? Employee { get; set; }

    public static CreateEmployeeResult Succeeded(EmployeeResponse employee)
    {
        return new CreateEmployeeResult
        {
            Success = true,
            Employee = employee
        };
    }

    public static CreateEmployeeResult Failed(
        string errorCode,
        string message)
    {
        return new CreateEmployeeResult
        {
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

public class CreateEmployeeHandler
    : IRequestHandler<CreateEmployeeCommand, CreateEmployeeResult>
{
    private static readonly string[] AllowedRoles =
    [
        EmployeeRoles.Admin,
        EmployeeRoles.Cashier,
        EmployeeRoles.InventoryEmployee
    ];

    private readonly IDbConnectionFactory _connectionFactory;

    public CreateEmployeeHandler(
        IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CreateEmployeeResult> Handle(
        CreateEmployeeCommand request,
        CancellationToken cancellationToken)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var username = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var role = request.Role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(role) && request.RoleId.HasValue)
        {
            role = request.RoleId.Value switch
            {
                1 => EmployeeRoles.Admin,
                2 => EmployeeRoles.Cashier,
                3 => EmployeeRoles.InventoryEmployee,
                _ => string.Empty
            };
        }

        var validationMessage = Validate(
            fullName,
            username,
            password,
            role);

        if (validationMessage is not null)
        {
            return CreateEmployeeResult.Failed(
                "ValidationError",
                validationMessage);
        }

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            const string usernameExistsSql = """
                SELECT COUNT(1)
                FROM Employees
                WHERE LOWER(Username) = LOWER(@Username);
                """;

            var usernameExistsCommand = new CommandDefinition(
                usernameExistsSql,
                new { Username = username },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var usernameExists =
                await connection.ExecuteScalarAsync<int>(
                    usernameExistsCommand) > 0;

            if (usernameExists)
            {
                transaction.Rollback();
                return CreateEmployeeResult.Failed(
                    "UsernameAlreadyExists",
                    "Username already exists.");
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            const string insertSql = """
                INSERT INTO Employees
                (
                    FullName,
                    Username,
                    PasswordHash,
                    Role,
                    IsActive,
                    CreatedAt
                )
                VALUES
                (
                    @FullName,
                    @Username,
                    @PasswordHash,
                    @Role,
                    TRUE,
                    UTC_TIMESTAMP()
                );

                SELECT LAST_INSERT_ID();
                """;

            var insertCommand = new CommandDefinition(
                insertSql,
                new
                {
                    FullName = fullName,
                    Username = username,
                    PasswordHash = passwordHash,
                    Role = role
                },
                transaction: transaction,
                cancellationToken: cancellationToken);

            var employeeId =
                await connection.ExecuteScalarAsync<long>(insertCommand);

            var permissionsToAssign = request.Permissions?.Distinct().ToList();
            if (permissionsToAssign is null || permissionsToAssign.Count == 0)
            {
                permissionsToAssign = GetDefaultPermissionsForRole(role);
            }

            if (permissionsToAssign.Count > 0)
            {
                const string insertPermSql = """
                    INSERT IGNORE INTO EmployeePermissions (EmployeeId, PermissionKey)
                    VALUES (@EmployeeId, @PermissionKey);
                    """;

                foreach (var perm in permissionsToAssign)
                {
                    if (PermissionKeys.IsValid(perm))
                    {
                        await connection.ExecuteAsync(
                            new CommandDefinition(
                                insertPermSql,
                                new { EmployeeId = employeeId, PermissionKey = perm },
                                transaction: transaction,
                                cancellationToken: cancellationToken));
                    }
                }
            }

            transaction.Commit();

            const string selectSql = """
                SELECT
                    Id,
                    FullName,
                    Username,
                    Role,
                    IsActive,
                    CreatedAt
                FROM Employees
                WHERE Id = @Id;
                """;

            var selectCommand = new CommandDefinition(
                selectSql,
                new { Id = employeeId },
                cancellationToken: cancellationToken);

            var employee =
                await connection.QuerySingleAsync<EmployeeResponse>(
                    selectCommand);

            return CreateEmployeeResult.Succeeded(employee);
        }
        catch (MySqlException exception)
            when (exception.Number == 1062)
        {
            transaction.Rollback();
            return CreateEmployeeResult.Failed(
                "UsernameAlreadyExists",
                "Username already exists.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static List<string> GetDefaultPermissionsForRole(string role)
    {
        if (string.Equals(role, EmployeeRoles.Cashier, StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                PermissionKeys.InvoicesCreate,
                PermissionKeys.InvoicesView,
                PermissionKeys.InvoicesReturn,
                PermissionKeys.InvoicesExchange,
                PermissionKeys.ReturnsExchange,
                PermissionKeys.SalesCreate,
                PermissionKeys.SalesView,
                PermissionKeys.CategoriesView,
                PermissionKeys.ProductsView,
                PermissionKeys.AttendanceViewEmployee,
                PermissionKeys.DashboardView
            };
        }

        if (string.Equals(role, EmployeeRoles.InventoryEmployee, StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>
            {
                PermissionKeys.ProductsView,
                PermissionKeys.ProductsCreate,
                PermissionKeys.ProductsUpdate,
                PermissionKeys.ProductsDeactivate,
                PermissionKeys.ProductsStockAdd,
                PermissionKeys.CategoriesView,
                PermissionKeys.CategoriesCreate,
                PermissionKeys.AttendanceViewEmployee,
                PermissionKeys.DashboardView
            };
        }

        if (string.Equals(role, EmployeeRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return PermissionKeys.All.ToList();
        }

        return new List<string>();
    }

    private static string? Validate(
        string fullName,
        string username,
        string password,
        string role)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return "Full name is required.";
        }

        if (fullName.Length > 150)
        {
            return "Full name must not exceed 150 characters.";
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return "Username is required.";
        }

        if (username.Length > 100)
        {
            return "Username must not exceed 100 characters.";
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return "Password is required.";
        }

        if (password.Length < 8)
        {
            return "Password must contain at least 8 characters.";
        }

        if (password.Length > 100)
        {
            return "Password must not exceed 100 characters.";
        }

        if (!AllowedRoles.Contains(
                role,
                StringComparer.OrdinalIgnoreCase))
        {
            return "Role must be Admin, Cashier, or InventoryEmployee.";
        }

        return null;
    }
}