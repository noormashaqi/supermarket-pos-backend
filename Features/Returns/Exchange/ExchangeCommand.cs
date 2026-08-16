using MediatR;

namespace SupermarketSystem.Api.Features.Returns.Exchange;

public record ExchangeItemInput(int ProductId, int Quantity);

public record ExchangeCommand(
    long OriginalInvoiceId,
    int OldProductId,
    int QuantityReturned,
    List<ExchangeItemInput> NewItems,
    long EmployeeId,
    string? Reason
) : IRequest<ExchangeResult>;

public record ExchangeResult(long ReturnId, long NewInvoiceId, string NewInvoiceNumber);