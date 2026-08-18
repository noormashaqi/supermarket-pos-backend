using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public record HeldInvoiceDto(
    long Id,
    string ReferenceTag,
    string? CustomerName,
    string? DiscountPercentage,
    string CartState,
    DateTime CreatedAt
);

public record GetHeldInvoicesQuery(long EmployeeId) : IRequest<List<HeldInvoiceDto>>;
