namespace Orders.Api.Shipping;

public sealed class Shipment
{
    private Shipment()
    {
        TrackingNumber = string.Empty;
    }

    public Shipment(Guid orderId, Carrier carrier, string trackingNumber, DateTimeOffset shippedAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("The order id must not be empty.", nameof(orderId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        OrderId = orderId;
        Carrier = carrier;
        TrackingNumber = trackingNumber;
        ShippedAt = shippedAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Carrier Carrier { get; private set; }

    public string TrackingNumber { get; private set; }

    public DateTimeOffset ShippedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public bool MarkDelivered(DateTimeOffset now)
    {
        if (DeliveredAt is not null)
        {
            return false;
        }

        DeliveredAt = now;
        return true;
    }
}
