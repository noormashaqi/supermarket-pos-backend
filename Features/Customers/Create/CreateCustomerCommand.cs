using MediatR;

namespace SupermarketSystem.Api.Features.Customers.Create;

public record CreateCustomerCommand(
    string FullName,
    string? Nickname,
    string? PhoneNumber
) : IRequest<CreateCustomerResult>;

public record CreateCustomerResult(long CustomerId);
