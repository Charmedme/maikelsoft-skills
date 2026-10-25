using FluentAssertions;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ShipmentTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    [DataRow("PostNL", "https://jouw.postnl.nl/track-and-trace/3S%20AB")]
    [DataRow("dhl", "https://www.dhl.com/nl-en/home/tracking.html?tracking-id=3S%20AB")]
    [DataRow("UPS", "https://tracking.example.com/UPS/3S%20AB")]
    public void TrackingUrl_DependsOnCarrier(string carrier, string expected)
    {
        var shipment = new Shipment(Guid.CreateVersion7(), carrier, "3S AB", s_now);

        shipment.TrackingUrl.AbsoluteUri.Should().Be(expected);
    }

    [TestMethod]
    public void Constructor_EmptyTrackingCode_Throws()
    {
        var act = () => new Shipment(Guid.CreateVersion7(), "DHL", " ", s_now);

        act.Should().Throw<ArgumentException>();
    }
}
