using FluentValidation;

namespace Orders.Api.Orders;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    private const int MaxLines = 20;

    public CreateOrderRequestValidator(IProductCatalog catalog)
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithErrorCode("order.customer_required");
        RuleFor(x => x.Lines).NotEmpty().WithErrorCode("order.lines_required");
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(MaxLines).WithErrorCode("order.lines_too_many");
        RuleFor(x => x.Lines)
            .Must(lines => lines.Select(l => l.Sku).Distinct().Count() == lines.Count)
            .WithErrorCode("order.sku_duplicate");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Sku).NotEmpty().WithErrorCode("order.sku_required");
            line.RuleFor(l => l.Sku).MustAsync(catalog.ExistsAsync).WithErrorCode("order.sku_unknown");
        });
    }
}
