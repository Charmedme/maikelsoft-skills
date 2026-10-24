using FluentAssertions;
using Orders.Api.Stock;

namespace Orders.Api.Tests;

[TestClass]
public sealed class StockLevelTests
{
    [TestMethod]
    public void Set_Negative_Throws()
    {
        var level = new StockLevel("SKU-1", 3, DateTimeOffset.UnixEpoch);

        var act = () => level.Set(-1, DateTimeOffset.UnixEpoch);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [TestMethod]
    public void Set_UpdatesTime()
    {
        var level = new StockLevel("SKU-1", 3, DateTimeOffset.UnixEpoch);
        var now = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

        level.Set(7, now);

        level.Available.Should().Be(7);
        level.UpdatedAt.Should().Be(now);
    }
}
