using MediatR;

namespace SupermarketSystem.Api.Features.Customers.Create;

public record CreateCustomerCommand(
    string? FullName,
    string? Name,
    string? Nickname,
    string? PhoneNumber,
    string? Phone
) : IRequest<CreateCustomerResult>;

public record CreateCustomerResult(
    long Id,
    long CustomerId,
    string FullName,
    string? Nickname,
    string? PhoneNumber
);
