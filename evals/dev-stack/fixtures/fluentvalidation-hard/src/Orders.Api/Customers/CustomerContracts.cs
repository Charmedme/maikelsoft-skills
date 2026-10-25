using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Customers;

public sealed record CreateCustomerRequest(
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required, StringLength(100, MinimumLength = 1)] string Name);

public sealed record CustomerResponse(Guid Id, string Email, string Name, DateTimeOffset CreatedAt);

public static class CustomerMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Email, customer.Name, customer.CreatedAt);
}
