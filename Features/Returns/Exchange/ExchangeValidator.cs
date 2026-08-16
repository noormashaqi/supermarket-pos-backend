using FluentValidation;

namespace SupermarketSystem.Api.Features.Returns.Exchange;

public class ExchangeValidator : AbstractValidator<ExchangeCommand>
{
    public ExchangeValidator()
    {
        RuleFor(x => x.OriginalInvoiceId).GreaterThan(0);
        RuleFor(x => x.OldProductId).GreaterThan(0);
        RuleFor(x => x.QuantityReturned).GreaterThan(0);
        RuleFor(x => x.EmployeeId).GreaterThan(0);

        // 1) التحقق من وجود عناصر جديدة داخل القائمة وأنها ليست فارغة
        RuleFor(x => x.NewItems)
            .NotNull().WithMessage("New items list cannot be null.")
            .NotEmpty().WithMessage("At least one replacement item must be provided.");

        // 2) التحقق من صحة بيانات كل عنصر داخل القائمة (Child Rules)
        RuleForEach(x => x.NewItems).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .GreaterThan(0).WithMessage("Product ID must be greater than 0.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        });
    }
}