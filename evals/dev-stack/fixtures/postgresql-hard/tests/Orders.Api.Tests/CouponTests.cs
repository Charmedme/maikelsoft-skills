using FluentAssertions;
using Orders.Api.Coupons;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CouponTests
{
    [TestMethod]
    public void Constructor_NormalizesCode() =>
        new Coupon("summer-26", 10m, 5, DateTimeOffset.UnixEpoch).Code.Should().Be("SUMMER-26");

    [TestMethod]
    public void UsesLeft_NewCoupon_IsMaxUses() =>
        new Coupon("A1", 10m, 5, DateTimeOffset.UnixEpoch).UsesLeft.Should().Be(5);

    [TestMethod]
    public void Constructor_MoreThanHundredPercent_Throws()
    {
        var act = () => new Coupon("A1", 101m, 5, DateTimeOffset.UnixEpoch);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
