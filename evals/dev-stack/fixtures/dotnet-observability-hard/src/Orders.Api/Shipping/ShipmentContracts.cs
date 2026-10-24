using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Shipping;

public sealed record CreateShipmentRequest([property: Required] Guid? OrderId);

public sealed record DispatchShipmentRequest(
    [property: Required, StringLength(16, MinimumLength = 2)] string Carrier);

public sealed record ShipmentResponse(
    Guid Id,
    Guid OrderId,
    string Status,
    string? Carrier,
    string? TrackingNumber,
    Uri? TrackingUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DispatchedAt);

public static class ShipmentMapping
{
    public static ShipmentResponse ToResponse(this Shipment shipment)
    {
        Uri? trackingUrl = null;
        if (shipment.Carrier is not null && shipment.TrackingNumber is not null && CarrierCatalog.TryGet(shipment.Carrier, out var carrier))
        {
            trackingUrl = CarrierCatalog.TrackingUrl(carrier, shipment.TrackingNumber);
        }

        return new(shipment.Id, shipment.OrderId, shipment.Status.ToString(), shipment.Carrier, shipment.TrackingNumber, trackingUrl, shipment.CreatedAt, shipment.DispatchedAt);
    }
}
