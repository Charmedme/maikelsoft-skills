namespace Orders.Api.Customers;

// The address where the warehouse sends the orders of a customer.
public sealed record DeliveryAddress(string Line, string PostalCode, string City, string CountryCode)
{
    public static DeliveryAddress From(AddressRequest request) =>
        new(request.Line.Trim(), request.PostalCode.Trim().ToUpperInvariant(), request.City.Trim(), request.CountryCode.Trim().ToUpperInvariant());
}
