using System.Collections.Frozen;
using FluentValidation;

namespace Orders.Api.Customers;

// The warehouse ships to the Netherlands, Belgium, and Germany.
public sealed class AddressRequestValidator : AbstractValidator<AddressRequest>
{
    private static readonly FrozenSet<string> s_countries = new[] { "NL", "BE", "DE" }.ToFrozenSet(StringComparer.Ordinal);

    public AddressRequestValidator()
    {
        RuleFor(x => x.Line)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("address.line_required")
            .MaximumLength(200).WithErrorCode("address.line_too_long");

        RuleFor(x => x.PostalCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("address.postal_code_required")
            .Matches("^[0-9A-Za-z][0-9A-Za-z -]{2,9}$").WithErrorCode("address.postal_code_invalid");

        RuleFor(x => x.City)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithErrorCode("address.city_required")
            .MaximumLength(80).WithErrorCode("address.city_too_long");

        RuleFor(x => x.CountryCode)
            .NotEmpty().WithErrorCode("address.country_required")
            .Must(code => s_countries.Contains(code.Trim().ToUpperInvariant())).WithErrorCode("address.country_not_supported")
            .WithMessage("We deliver to NL, BE, and DE only.");
    }
}
