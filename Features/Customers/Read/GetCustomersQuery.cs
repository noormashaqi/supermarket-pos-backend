using MediatR;

namespace SupermarketSystem.Api.Features.Customers.Read;

public record GetCustomersQuery() : IRequest<List<CustomerDto>>;

public class CustomerDto
{
    public long Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Nickname { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal CurrentBalance { get; init; }
    public DateTime CreatedAt { get; init; }
}
