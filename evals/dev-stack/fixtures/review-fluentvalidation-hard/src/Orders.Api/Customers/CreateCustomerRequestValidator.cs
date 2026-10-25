using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Customers;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator(OrdersDbContext db, IValidator<AddressRequest> addressValidator)
    {
        // Stop at the first failure, so that the customer gets one message for each field.
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().MaximumLength(256).WithErrorCode("customer.email_invalid")
            .MustAsync(async (email, ct) =>
            {
                var normalized = email.Trim().ToLowerInvariant();
                return !await db.Customers.AnyAsync(c => c.Email == normalized, ct);
            })
            .WithErrorCode("customer.email_taken")
            .WithMessage("A customer with this email address exists.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("customer.name_required")
            .MaximumLength(100).WithErrorCode("customer.name_too_long");

        RuleFor(x => x.Address).SetValidator(addressValidator);
    }
}
