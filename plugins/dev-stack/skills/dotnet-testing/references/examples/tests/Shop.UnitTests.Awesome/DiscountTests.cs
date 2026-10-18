using AwesomeAssertions;

namespace Shop.UnitTests.Awesome;

[TestClass]
public sealed class DiscountTests
{
    [TestMethod]
    [DataRow(99.99, 0.0, DisplayName = "Below first tier")]
    [DataRow(100.0, 5.0, DisplayName = "First tier boundary")]
    [DataRow(1000.0, 100.0, DisplayName = "Second tier boundary")]
    public void For_TotalAtTier_ReturnsTierDiscount(double total, double expected)
    {
        decimal discount = Discount.For((decimal)total);

        discount.Should().Be((decimal)expected);
    }

    [TestMethod]
    [DynamicData(nameof(LargeTotals))]
    public void For_LargeTotal_ReturnsTenPercent(decimal total, decimal expected)
    {
        decimal discount = Discount.For(total);

        discount.Should().Be(expected);
    }

    [TestMethod]
    public void For_NegativeTotal_ThrowsArgumentOutOfRange()
    {
        Action act = () => Discount.For(-1m);

        act.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("total");
    }

    public static IEnumerable<(decimal Total, decimal Expected)> LargeTotals =>
    [
        (1000m, 100m),
        (2500m, 250m),
    ];
}
