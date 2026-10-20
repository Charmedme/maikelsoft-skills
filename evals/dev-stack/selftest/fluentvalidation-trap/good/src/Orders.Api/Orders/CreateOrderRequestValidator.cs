using FluentValidation;
using Microsoft.Extensions.Options;

namespace Orders.Api.Orders;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator(IOptions<OrdersOptions> options, IValidator<CreateOrderLineRequest> lineValidator)
    {
        var maxLines = options.Value.MaxLinesPerOrder;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithErrorCode("order.customer_required");

        RuleFor(x => x.Lines)
            .NotEmpty().WithErrorCode("order.lines_required")
            .Must(lines => lines.Count <= maxLines).WithErrorCode("order.lines_too_many")
            .WithMessage($"An order can have at most {maxLines} lines.")
            .Must(HaveUniqueSkus).WithErrorCode("order.sku_duplicate")
            .WithMessage("Each SKU can appear only once in an order.");

        RuleForEach(x => x.Lines).SetValidator(lineValidator);
    }

    private static bool HaveUniqueSkus(IReadOnlyList<CreateOrderLineRequest> lines)
    {
        var skus = lines.Where(line => line is not null && !string.IsNullOrWhiteSpace(line.Sku)).Select(line => line.Sku.Trim()).ToList();
        return skus.Distinct(StringComparer.OrdinalIgnoreCase).Count() == skus.Count;
    }
}

public sealed class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator(IProductCatalog catalog)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Sku)
            .NotEmpty().WithErrorCode("order.sku_required")
            .MaximumLength(32).WithErrorCode("order.sku_too_long")
            .MustAsync(catalog.ExistsAsync).WithErrorCode("order.sku_unknown")
            .WithMessage("The SKU is not a known product.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, 1000).WithErrorCode("order.quantity_out_of_range");

        RuleFor(x => x.UnitPrice)
            .InclusiveBetween(0m, 100_000m).WithErrorCode("order.unit_price_out_of_range");
    }
}
