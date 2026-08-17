using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Read;

public record GetInvoiceByIdQuery(long Id) : IRequest<InvoiceDto?>;