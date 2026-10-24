using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Customers;

// Validated by CreateCustomerRequestValidator.
public sealed record CreateCustomerRequest(string Email, string Name, AddressRequest Address);

// Validated by UpdateCustomerRequestValidator. A null address keeps the current delivery address.
public sealed record UpdateCustomerRequest(
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required, StringLength(100, MinimumLength = 1)] string Name,
    AddressRequest? Address);

public sealed record AddressRequest(string Line, string PostalCode, string City, string CountryCode);

public sealed record CustomerResponse(Guid Id, string Email, string Name, AddressResponse Address, DateTimeOffset CreatedAt);

public sealed record AddressResponse(string Line, string PostalCode, string City, string CountryCode);

public static class CustomerMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(
            customer.Id,
            customer.Email,
            customer.Name,
            new AddressResponse(customer.AddressLine, customer.PostalCode, customer.City, customer.CountryCode),
            customer.CreatedAt);
}
