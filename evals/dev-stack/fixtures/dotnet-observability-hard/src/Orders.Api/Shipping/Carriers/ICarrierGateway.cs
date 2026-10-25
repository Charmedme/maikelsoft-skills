namespace Orders.Api.Shipping.Carriers;

// Books parcels with the carriers through the carrier gateway. See docs/carrier-gateway-api.md.
public interface ICarrierGateway
{
    Task<CarrierBooking> BookAsync(CarrierBookingRequest request, CancellationToken ct);
}
