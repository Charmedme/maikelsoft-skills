using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderTests
{
    private static readonly TimeSpan s_window = TimeSpan.FromHours(24);

    [TestMethod]
    public void Total_SumsAllLines()
    {
        var order = new Order(Guid.CreateVersion7(), DateTimeOffset.UtcNow);
        order.AddLine("SKU-1", 2, 10.50m);
        order.AddLine("SKU-2", 1, 4.25m);

        Assert.AreEqual(order.Total, 25.25m);
    }

    [TestMethod]
    public void OrderLine_Constructor_SetsProperties()
    {
        var line = new OrderLine("SKU-1", 2, 3m);

        line.Sku.Should().Be("SKU-1");
        line.Quantity.Should().Be(2);
        line.UnitPrice.Should().Be(3m);
    }

    [TestMethod]
    public void Cancel_Works()
    {
        var placedAt = DateTimeOffset.UtcNow;
        var order = new Order(Guid.CreateVersion7(), placedAt);

        Thread.Sleep(1000);

        var act = () => order.Cancel(DateTimeOffset.UtcNow, s_window);

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Cancel_AllCases()
    {
        var hours = new[] { 1, 23, 25, 48 };
        foreach (var hour in hours)
        {
            var placedAt = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
            var order = new Order(Guid.CreateVersion7(), placedAt);
            var outcome = order.Cancel(placedAt.AddHours(hour), s_window);
            if (hour < 24)
            {
                outcome.Should().Be(CancelOutcome.Cancelled);
            }
            else
            {
                outcome.Should().Be(CancelOutcome.WindowClosed);
            }
        }
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void AddLine_InvalidQuantity_Throws(int quantity)
    {
        var order = new Order(Guid.CreateVersion7(), DateTimeOffset.UtcNow);

        var act = () => order.AddLine("SKU-1", quantity, 1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
