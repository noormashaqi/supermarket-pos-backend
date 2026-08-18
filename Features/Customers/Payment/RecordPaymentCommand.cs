using MediatR;

namespace SupermarketSystem.Api.Features.Customers.Payment;

public record RecordPaymentCommand(
    long CustomerId,
    decimal Amount,
    int EmployeeId,
    string? Notes
) : IRequest<RecordPaymentResult>;

public record RecordPaymentResult(long PaymentId, decimal NewBalance);
