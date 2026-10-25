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

    // Why the carrier refused the address. Customer service reads it.
    public string? ProblemReason { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public void Book(string carrier, string trackingNumber, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrier);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingNumber);
        if (!CanBeBooked)
        {
            throw new InvalidOperationException("Only a pending shipment or a shipment with an address problem can be booked.");
        }

        Carrier = carrier;
        TrackingNumber = trackingNumber;
        BookedAt = now;
        ProblemReason = null;
        Status = ShipmentStatus.Booked;
    }

    public bool CanBeBooked => Status is ShipmentStatus.Pending or ShipmentStatus.AddressProblem;

    public void MarkAddressProblem(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!CanBeBooked)
        {
            throw new InvalidOperationException("Only a shipment that is not booked can have an address problem.");
        }

        ProblemReason = reason.Length > 500 ? reason[..500] : reason;
        Status = ShipmentStatus.AddressProblem;
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
