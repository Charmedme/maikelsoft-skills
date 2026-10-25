using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Customers;

public sealed record CreateCustomerRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required] CustomerKind? Kind,
    [property: StringLength(200)] string? CompanyName);

public sealed record ChangeEmailRequest(
    [property: Required, EmailAddress, StringLength(254)] string Email);

public sealed record BrandingRequest(
    Uri? LogoUrl,
    [property: StringLength(200)] string? FooterText);

public sealed record BrandingResponse(Uri? LogoUrl, string? FooterText);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string Kind,
    string? CompanyName,
    DateTimeOffset CreatedAt);

public static class CustomerMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Kind.ToString(), customer.CompanyName, customer.CreatedAt);
}
