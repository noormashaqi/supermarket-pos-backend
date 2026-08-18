using FluentValidation;

namespace SupermarketSystem.Api.Features.Customers.Payment;

public class RecordPaymentValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("A valid customer ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);
    }
}
