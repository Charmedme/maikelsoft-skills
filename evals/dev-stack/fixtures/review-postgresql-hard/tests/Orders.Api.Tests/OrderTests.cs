using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Total_AppliesDiscountAndRoundsToCents()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);
        order.AddLine("SKU-1", 3, 3.33m);
        order.ApplyCoupon("SAVE", 15m);

        order.Total.Should().Be(8.49m);
    }

    [TestMethod]
    public void Cancel_AfterWindow_ReturnsWindowClosed()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);

        order.Cancel(s_now.AddHours(25), TimeSpan.FromHours(24)).Should().Be(CancelOutcome.WindowClosed);
        order.Status.Should().Be(OrderStatus.Placed);
    }

    [TestMethod]
    public void Cancel_Twice_ReturnsAlreadyCancelled()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);
        order.Cancel(s_now, TimeSpan.FromHours(24));

        order.Cancel(s_now, TimeSpan.FromHours(24)).Should().Be(CancelOutcome.AlreadyCancelled);
    }

    [TestMethod]
    public void Cancel_WhilePicking_ReturnsAlreadyShipped()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);
        order.StartPicking("w-1", s_now);

        order.Cancel(s_now, TimeSpan.FromHours(24)).Should().Be(CancelOutcome.AlreadyShipped);
    }

    [TestMethod]
    public void StartPicking_NotPlaced_Throws()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);
        order.Cancel(s_now, TimeSpan.FromHours(24));

        var act = () => order.StartPicking("w-1", s_now);

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void AddLine_AfterCancel_Throws()
    {
        var order = new Order(Guid.CreateVersion7(), s_now);
        order.Cancel(s_now, TimeSpan.FromHours(24));

        var act = () => order.AddLine("SKU-1", 1, 1m);

        act.Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public void Constructor_EmptyCustomer_Throws()
    {
        var act = () => new Order(Guid.Empty, s_now);

        act.Should().Throw<ArgumentException>();
    }
}
