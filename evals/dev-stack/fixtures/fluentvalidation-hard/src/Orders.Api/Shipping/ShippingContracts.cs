using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Shipping;

public sealed record ShipOrderRequest(
    [property: Required, StringLength(32, MinimumLength = 2)] string Carrier,
    [property: Required, StringLength(64, MinimumLength = 4)] string TrackingCode);

public sealed record ShipmentResponse(Guid Id, Guid OrderId, string Carrier, string TrackingCode, Uri TrackingUrl, DateTimeOffset ShippedAt);

public static class ShipmentMapping
{
    public static ShipmentResponse ToResponse(this Shipment shipment) =>
        new(shipment.Id, shipment.OrderId, shipment.Carrier, shipment.TrackingCode, shipment.TrackingUrl, shipment.ShippedAt);
}
