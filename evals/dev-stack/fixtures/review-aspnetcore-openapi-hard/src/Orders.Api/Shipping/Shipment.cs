namespace Orders.Api.Shipping;

public sealed class Shipment
{
    private Shipment()
    {
        Carrier = string.Empty;
        TrackingCode = string.Empty;
    }

    public Shipment(Guid orderId, string carrier, string trackingCode, DateTimeOffset shippedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(carrier);
        ArgumentException.ThrowIfNullOrWhiteSpace(trackingCode);

        OrderId = orderId;
        Carrier = carrier.Trim();
        TrackingCode = trackingCode.Trim();
        ShippedAt = shippedAt;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Carrier { get; private set; }

    public string TrackingCode { get; private set; }

    public DateTimeOffset ShippedAt { get; private set; }

    public Uri TrackingUrl => Carrier.ToUpperInvariant() switch
    {
        "POSTNL" => new Uri($"https://jouw.postnl.nl/track-and-trace/{Uri.EscapeDataString(TrackingCode)}"),
        "DHL" => new Uri($"https://www.dhl.com/nl-en/home/tracking.html?tracking-id={Uri.EscapeDataString(TrackingCode)}"),
        _ => new Uri($"https://tracking.example.com/{Uri.EscapeDataString(Carrier)}/{Uri.EscapeDataString(TrackingCode)}"),
    };
}
