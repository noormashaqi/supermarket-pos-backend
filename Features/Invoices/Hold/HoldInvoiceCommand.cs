using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public record HoldInvoiceCommand(
    long EmployeeId,
    string ReferenceTag,
    string? CustomerName,
    string? DiscountPercentage,
    string CartState
) : IRequest<HoldInvoiceResult>;

public record HoldInvoiceResult(long Id, string ReferenceTag);
