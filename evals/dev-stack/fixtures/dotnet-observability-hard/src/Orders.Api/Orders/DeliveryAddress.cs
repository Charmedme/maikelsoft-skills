namespace Orders.Api.Orders;

// The address that the carrier delivers to. It comes from the webshop checkout.
public sealed record DeliveryAddress(
    string RecipientName,
    string Street,
    string PostalCode,
    string City,
    string CountryCode,
    string? Phone);
