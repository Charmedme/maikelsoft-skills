using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Customers;

public sealed record CreateCustomerRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Required, EmailAddress, StringLength(254)] string Email,
    [property: Required] CustomerKind? Kind,
    [property: StringLength(200)] string? CompanyName);

public sealed record ChangeEmailRequest(
    [property: Required, EmailAddress, StringLength(254)] string Email);

public sealed record NotificationAddressesRequest(
    [property: Required, MaxLength(5)] IReadOnlyList<string> Addresses);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string Email,
    string Kind,
    string? CompanyName,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> NotificationAddresses);

public static class CustomerMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.Kind.ToString(), customer.CompanyName, customer.CreatedAt, customer.NotificationAddresses);
}
