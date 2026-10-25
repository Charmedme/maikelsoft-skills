using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderTests
{
    private static readonly DateTimeOffset s_placedAt = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan s_window = TimeSpan.FromHours(24);

    [TestMethod]
    public void Total_SumsAllLines()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.AddLine("SKU-1", 2, 10.50m);
        order.AddLine("SKU-2", 1, 4.25m);

        order.Total.Should().Be(25.25m);
    }

    [TestMethod]
    public void Cancel_InsideWindow_CancelsOrder()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);

        var outcome = order.Cancel(s_placedAt.AddHours(2), s_window);

        outcome.Should().Be(CancelOutcome.Cancelled);
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().Be(s_placedAt.AddHours(2));
    }

    [TestMethod]
    public void Cancel_AfterWindow_KeepsOrderPlaced()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);

        var outcome = order.Cancel(s_placedAt.AddHours(25), s_window);

        outcome.Should().Be(CancelOutcome.WindowClosed);
        order.Status.Should().Be(OrderStatus.Placed);
    }

    [TestMethod]
    public void Cancel_Shipped_ReturnsAlreadyShipped()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.MarkShipped();

        order.Cancel(s_placedAt.AddHours(1), s_window).Should().Be(CancelOutcome.AlreadyShipped);
    }

    [TestMethod]
    public void Constructor_KeepsReferenceAndInstructions()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt, "PO-1", "Ring twice.");

        order.CustomerReference.Should().Be("PO-1");
        order.DeliveryInstructions.Should().Be("Ring twice.");
    }

    [TestMethod]
    public void AddLine_ZeroQuantity_Throws()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);

        var act = () => order.AddLine("SKU-1", 0, 1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void MarkPaid_Placed_SetsPaidAt()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);

        order.MarkPaid(s_placedAt.AddMinutes(5));

        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().Be(s_placedAt.AddMinutes(5));
    }

    [TestMethod]
    public void MarkPaid_Twice_KeepsFirstCapture()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.MarkPaid(s_placedAt.AddMinutes(5));

        order.MarkPaid(s_placedAt.AddMinutes(9));

        order.PaidAt.Should().Be(s_placedAt.AddMinutes(5));
    }

    [TestMethod]
    public void MarkPaid_Cancelled_Throws()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.Cancel(s_placedAt.AddHours(1), s_window);

        var act = () => order.MarkPaid(s_placedAt.AddHours(2));

        act.Should().Throw<InvalidOperationException>();
    }
}
