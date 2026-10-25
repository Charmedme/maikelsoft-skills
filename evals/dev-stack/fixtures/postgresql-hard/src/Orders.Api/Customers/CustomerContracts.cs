using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Customers;

public sealed record CreateCustomerRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Required, EmailAddress, StringLength(320)] string Email);

public sealed record RenameCustomerRequest([property: Required, StringLength(200, MinimumLength = 1)] string Name);

public sealed record CustomerResponse(Guid Id, string Name, string Email, DateTimeOffset CreatedAt);

public static class CustomerMapping
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.Id, customer.Name, customer.Email, customer.CreatedAt);
}
