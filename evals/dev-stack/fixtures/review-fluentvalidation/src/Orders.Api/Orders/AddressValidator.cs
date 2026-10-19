using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Orders;

internal sealed class AddressValidator : AbstractValidator<ChangeAddressRequest>
{
    public AddressValidator(IPostalCodeLookup postalCodes, OrdersDbContext db)
    {
        RuleFor(x => x.OrderId)
            .MustAsync(async (orderId, ct) => await db.Orders.AnyAsync(o => o.Id == orderId && o.Status == OrderStatus.Placed, ct))
            .WithErrorCode("order.address_locked")
            .WithMessage("The address of a shipped or cancelled order cannot change.");

        RuleFor(x => x.Street)
            .NotEmpty().WithErrorCode("address.street_required")
            .MaximumLength(100).WithErrorCode("address.street_too_long");
        RuleFor(x => x.City)
            .NotEmpty().WithErrorCode("address.city_required")
            .MaximumLength(60).WithErrorCode("address.city_too_long");
        RuleFor(x => x.Country)
            .Equal("NL").WithErrorCode("address.country_not_supported")
            .WithMessage("We ship to the Netherlands only.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithErrorCode("address.postal_code_required")
            .Matches("^[0-9]{4} ?[A-Z]{2}$").WithErrorCode("address.postal_code_invalid");

        RuleFor(x => x)
            .MustAsync((request, ct) => postalCodes.ExistsAsync(request.Country, request.PostalCode, ct))
            .WithErrorCode("address.postal_code_unknown")
            .WithMessage("The postal code does not exist.");
    }
}
