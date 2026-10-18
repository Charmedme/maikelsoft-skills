namespace Orders.Api.Shipments;

public sealed class Shipment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    public string Carrier { get; set; } = string.Empty;

    public string TrackingCode { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public DateTime ShippedAt { get; set; }

    public Address Destination { get; set; } = new();
}

public sealed class Address
{
    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;
}
