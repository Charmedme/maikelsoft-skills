using FluentValidation;
using Microsoft.Extensions.Options;
using Orders.Api.Catalog;

namespace Orders.Api.Orders;

// The rules for a new order, from POST /orders and from the CSV import. Both show the error code of each rule.
public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IOptions<OrdersOptions> options, IValidator<CreateOrderLineRequest> lineValidator)
    {
        var maxLines = options.Value.MaxLinesPerOrder;

        // Stop each rule chain at its first failure: a null list must not reach Count, an empty SKU must not reach the catalog.
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CustomerId).NotEmpty().WithErrorCode("order.customer_required");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("order.lines_required")
            .Must(lines => lines.Count <= maxLines).WithErrorCode("order.lines_too_many")
            .WithMessage($"An order can have at most {maxLines} lines.")
            .Must(HaveUniqueSkus).WithErrorCode("order.sku_duplicate")
            .WithMessage("Each SKU can appear only once in an order.");

        // SetValidator skips a null item, so check it first.
        RuleForEach(x => x.Lines)
            .NotNull().WithErrorCode("order.line_required")
            .SetValidator(lineValidator);
    }

    private static bool HaveUniqueSkus(IReadOnlyList<CreateOrderLineRequest> lines)
    {
        var skus = lines
            .Where(line => line is not null && !string.IsNullOrWhiteSpace(line.Sku))
            .Select(line => Product.Normalize(line.Sku))
            .ToList();
        return skus.Distinct(StringComparer.Ordinal).Count() == skus.Count;
    }
}

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator(IProductCatalog catalog)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        // WithErrorCode applies to the validator just before it, so each check has its own code.
        RuleFor(x => x.Sku)
            .NotEmpty().WithErrorCode("order.sku_required")
            .MaximumLength(32).WithErrorCode("order.sku_invalid")
            .MustAsync((sku, ct) => catalog.IsActiveAsync(sku, ct)).WithErrorCode("order.sku_unknown")
            .WithMessage("The SKU is not an active product.");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000).WithErrorCode("order.quantity_out_of_range");
        RuleFor(x => x.UnitPrice).InclusiveBetween(0m, 100_000m).WithErrorCode("order.unit_price_out_of_range");
    }
}
