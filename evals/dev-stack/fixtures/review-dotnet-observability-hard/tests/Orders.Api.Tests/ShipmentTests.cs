using FluentAssertions;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ShipmentTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Dispatch_Pending_SetsCarrierAndTime()
    {
        var shipment = new Shipment(Guid.CreateVersion7(), s_now);

        shipment.Dispatch("postnl", "3S123", s_now.AddHours(1));

        shipment.Status.Should().Be(ShipmentStatus.Dispatched);
        shipment.Carrier.Should().Be("postnl");
        shipment.DispatchedAt.Should().Be(s_now.AddHours(1));
    }

    [TestMethod]
    public void MarkDelivered_Pending_Throws()
    {
        var shipment = new Shipment(Guid.CreateVersion7(), s_now);

        var act = () => shipment.MarkDelivered(s_now);

        act.Should().Throw<InvalidOperationException>();
    }
}
