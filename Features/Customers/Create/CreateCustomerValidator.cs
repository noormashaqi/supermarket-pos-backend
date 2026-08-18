using FluentValidation;

namespace SupermarketSystem.Api.Features.Customers.Create;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Customer full name is required.")
            .MaximumLength(150);

        RuleFor(x => x.Nickname)
            .MaximumLength(100)
            .When(x => x.Nickname is not null);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .When(x => x.PhoneNumber is not null);
    }
}
