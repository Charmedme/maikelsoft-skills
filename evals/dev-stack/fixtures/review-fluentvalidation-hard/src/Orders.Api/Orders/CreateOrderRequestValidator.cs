using FluentValidation;
using Microsoft.Extensions.Options;

namespace Orders.Api.Orders;

// The rules for an order from the CSV import. The import report shows the error code of each rule.
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IOptions<OrdersOptions> options)
    {
        var maxLines = options.Value.MaxLinesPerOrder;

        RuleFor(x => x.CustomerId).NotEmpty().WithErrorCode("order.customer_required");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("order.lines_required")
            .Must(lines => lines.Count <= maxLines).WithErrorCode("order.lines_too_many")
            .WithMessage($"An order can have at most {maxLines} lines.");

        RuleForEach(x => x.Lines).SetValidator(new CreateOrderLineRequestValidator());
    }
}

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(32).WithErrorCode("order.sku_invalid");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000).WithErrorCode("order.quantity_out_of_range");
        RuleFor(x => x.UnitPrice).InclusiveBetween(0m, 100_000m).WithErrorCode("order.unit_price_out_of_range");
    }
}
