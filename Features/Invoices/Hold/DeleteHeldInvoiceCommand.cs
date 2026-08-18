using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Hold;

public record DeleteHeldInvoiceCommand(long Id, long EmployeeId) : IRequest<bool>;
