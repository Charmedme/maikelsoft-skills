using Orders.Api.Contracts;
using Orders.Api.Orders;

namespace Orders.Api.Shipping;

public interface ICarrierClient
{
    /// <summary>Books a parcel. Throws <see cref="CarrierAddressRejectedException"/> when the carrier cannot deliver to the address.</summary>
    Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct);
}

/// <param name="Reference">Our reference for the parcel. The platform uses it as the idempotency key.</param>
public sealed record CarrierBookingRequest(string Reference, string RecipientName, DeliveryAddress Address, ShippingSpeed Speed);

public sealed record CarrierBooking(string BookingId, string Carrier, string TrackingNumber);
