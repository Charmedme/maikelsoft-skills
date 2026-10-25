using FluentValidation;

namespace Orders.Api.Customers;

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator(IValidator<AddressRequest> addressValidator)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("customer.email_required")
            .EmailAddress().WithErrorCode("customer.email_invalid")
            .MaximumLength(256).WithErrorCode("customer.email_too_long");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("customer.name_required")
            .MaximumLength(100).WithMessage("The name can have at most 100 characters.").WithErrorCode("customer.name_too_long");

        // The address is optional here: null keeps the current delivery address.
        RuleFor(x => x.Address!).SetValidator(addressValidator);
    }
}
