using FluentValidation;

namespace SupermarketSystem.Api.Features.Invoices.Create;

public class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    private static readonly string[] ValidPaymentMethods = ["Cash", "Debt"];

    public CreateInvoiceValidator()
    {
        RuleFor(x => x.EmployeeId)
            .GreaterThan(0);

        RuleFor(x => x.DiscountPercentage)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Invoice must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).GreaterThan(0);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.PaymentMethod)
            .Must(pm => pm is null || ValidPaymentMethods.Contains(pm, StringComparer.OrdinalIgnoreCase))
            .WithMessage("PaymentMethod must be 'Cash' or 'Debt'.");

        RuleFor(x => x.CustomerId)
            .NotNull()
            .WithMessage("CustomerId is required for debt sales.")
            .GreaterThan(0)
            .WithMessage("CustomerId must be a valid positive number.")
            .When(x => string.Equals(x.PaymentMethod, "Debt", StringComparison.OrdinalIgnoreCase));
    }
}