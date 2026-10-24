using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderLineTests
{
    [TestMethod]
    public void LineTotal_IsQuantityTimesPrice() => new OrderLine("SKU-1", 4, 1.25m).LineTotal.Should().Be(5m);

    [TestMethod]
    public void Constructor_ZeroQuantity_Throws()
    {
        var act = () => new OrderLine("SKU-1", 0, 1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Constructor_NegativePrice_Throws()
    {
        var act = () => new OrderLine("SKU-1", 1, -0.01m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
