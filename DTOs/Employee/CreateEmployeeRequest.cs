namespace SupermarketSystem.Api.Dtos.Employees;

public class CreateEmployeeRequest
{
    public string FullName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? RoleId { get; set; }

    public List<string>? Permissions { get; set; }
}