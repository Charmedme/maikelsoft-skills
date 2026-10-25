using FluentAssertions;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ShipmentTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void MarkDelivered_Twice_KeepsFirstTime()
    {
        var shipment = new Shipment(Guid.CreateVersion7(), Carrier.Dpd, "0123456789", s_now);

        shipment.MarkDelivered(s_now.AddDays(1)).Should().BeTrue();
        shipment.MarkDelivered(s_now.AddDays(2)).Should().BeFalse();

        shipment.DeliveredAt.Should().Be(s_now.AddDays(1));
    }

    [TestMethod]
    public void Constructor_EmptyTrackingNumber_Throws()
    {
        var act = () => new Shipment(Guid.CreateVersion7(), Carrier.Dpd, " ", s_now);

        act.Should().Throw<ArgumentException>();
    }
}
