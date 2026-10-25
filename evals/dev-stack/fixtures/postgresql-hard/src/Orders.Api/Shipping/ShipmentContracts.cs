using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Shipping;

public sealed record CreateShipmentRequest(
    [property: Required] Guid? OrderId,
    [property: Required] Carrier? Carrier,
    [property: Required, StringLength(40, MinimumLength = 4), RegularExpression("^[A-Za-z0-9-]+$")] string TrackingNumber);

public sealed record ShipmentResponse(Guid Id, Guid OrderId, string Carrier, string TrackingNumber, DateTimeOffset ShippedAt, DateTimeOffset? DeliveredAt);

public static class ShipmentMapping
{
    public static ShipmentResponse ToResponse(this Shipment shipment) =>
        new(shipment.Id, shipment.OrderId, shipment.Carrier.ToString(), shipment.TrackingNumber, shipment.ShippedAt, shipment.DeliveredAt);
}
