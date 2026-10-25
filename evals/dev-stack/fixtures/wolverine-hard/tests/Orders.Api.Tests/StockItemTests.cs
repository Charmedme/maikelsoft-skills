using FluentAssertions;
using Orders.Api.Inventory;

namespace Orders.Api.Tests;

[TestClass]
public sealed class StockItemTests
{
    [TestMethod]
    public void TryReserve_MoreThanAvailable_ReturnsFalse()
    {
        var item = new StockItem("SKU-1");
        item.Receive(5);

        item.TryReserve(6).Should().BeFalse();
        item.Reserved.Should().Be(0);
    }

    [TestMethod]
    public void Ship_ReducesOnHandAndReserved()
    {
        var item = new StockItem("SKU-1");
        item.Receive(5);
        item.TryReserve(3);

        item.Ship(3);

        item.OnHand.Should().Be(2);
        item.Reserved.Should().Be(0);
        item.Available.Should().Be(2);
    }

    [TestMethod]
    public void Release_MoreThanReserved_StopsAtZero()
    {
        var item = new StockItem("SKU-1");
        item.Receive(5);
        item.TryReserve(2);

        item.Release(4);

        item.Reserved.Should().Be(0);
    }
}
