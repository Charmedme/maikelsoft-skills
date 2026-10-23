using FluentValidation;

namespace Orders.Api.Returns;

public sealed class CreateReturnRequestValidator : AbstractValidator<CreateReturnRequest>
{
    public CreateReturnRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithErrorCode("return.order_required");
        RuleFor(x => x.Sku).NotEmpty().WithErrorCode("return.sku_required");
        RuleFor(x => x.Sku).MaximumLength(32).WithErrorCode("return.sku_too_long");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 1000).WithErrorCode("return.quantity_out_of_range");
        RuleFor(x => x.Reason).NotNull().WithErrorCode("return.reason_required");
        RuleFor(x => x.Reason).IsInEnum().WithErrorCode("return.reason_unknown");
        RuleFor(x => x.Comment).MaximumLength(500).WithErrorCode("return.comment_too_long");
    }
}
