using FluentValidation;
using Microsoft.Extensions.Options;
using Orders.Api.Catalog;

namespace Orders.Api.Orders;

// The rules for a new order, from POST /orders and from the CSV import.
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IOptions<OrdersOptions> options, IValidator<CreateOrderLineRequest> lineValidator)
    {
        var maxLines = options.Value.MaxLinesPerOrder;

        RuleFor(x => x.CustomerId).NotEmpty().WithErrorCode("order.customer_required");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("order.lines_required")
            .Must(lines => lines.Count <= maxLines).WithErrorCode("order.lines_too_many")
            .WithMessage($"An order can have at most {maxLines} lines.")
            .Must(lines => lines.Select(line => line.Sku).Distinct().Count() == lines.Count).WithErrorCode("order.sku_duplicate")
            .WithMessage("A product can be on an order only once.");

        RuleForEach(x => x.Lines).SetValidator(lineValidator);
    }
}

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator(IProductCatalog catalog)
    {
        RuleFor(x => x.Sku)
            .NotEmpty().MaximumLength(32).WithErrorCode("order.sku_invalid")
            .MustAsync((sku, ct) => catalog.IsActiveAsync(sku, ct)).WithErrorCode("order.sku_unknown")
            .WithMessage("We do not sell this product.");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000).WithErrorCode("order.quantity_out_of_range");
        RuleFor(x => x.UnitPrice).InclusiveBetween(0m, 100_000m).WithErrorCode("order.unit_price_out_of_range");
    }
}
