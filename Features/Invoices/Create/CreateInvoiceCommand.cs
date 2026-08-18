using MediatR;

namespace SupermarketSystem.Api.Features.Invoices.Create;

public record CreateInvoiceItemDto(int ProductId, int Quantity, decimal? UnitPrice = null);

public record CreateInvoiceCommand(
    int EmployeeId,
    decimal DiscountPercentage,
    List<CreateInvoiceItemDto> Items
) : IRequest<CreateInvoiceResult>;

public record CreateInvoiceResult(int InvoiceId, string InvoiceNumber);