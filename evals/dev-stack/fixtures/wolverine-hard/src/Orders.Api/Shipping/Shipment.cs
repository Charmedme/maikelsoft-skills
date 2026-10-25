namespace Orders.Api.Shipping;

public sealed class Shipment
{
    private Shipment()
    {
    }

    public Shipment(Guid orderId, DateTimeOffset createdAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("The order id must not be empty.", nameof(orderId));
        }

        OrderId = orderId;
        CreatedAt = createdAt;
        Status = ShipmentStatus.Pending;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public string? Carrier { get; private set; }

    public string? TrackingNumber { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? BookedAt { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public void Book(string carrier, string trackingNumber, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrier);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        if (Status != ShipmentStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending shipment can be booked.");
        }

        Carrier = carrier;
        TrackingNumber = trackingNumber;
        BookedAt = now;
        Status = ShipmentStatus.Booked;
    }

    public void Dispatch(string carrier, string trackingNumber, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrier);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        if (Status is not (ShipmentStatus.Pending or ShipmentStatus.Booked))
        {
            throw new InvalidOperationException("Only a pending or booked shipment can be dispatched.");
        }

        Carrier = carrier;
        TrackingNumber = trackingNumber;
        DispatchedAt = now;
        Status = ShipmentStatus.Dispatched;
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        if (Status != ShipmentStatus.Dispatched)
        {
            throw new InvalidOperationException("Only a dispatched shipment can be delivered.");
        }

        DeliveredAt = now;
        Status = ShipmentStatus.Delivered;
    }
}
