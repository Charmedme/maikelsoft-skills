using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Coupons;

public sealed record CreateCouponRequest(
    [property: Required, StringLength(32, MinimumLength = 3), RegularExpression("^[A-Za-z0-9-]+$")] string Code,
    [property: Range(typeof(decimal), "0.01", "100")] decimal PercentOff,
    [property: Range(1, 100000)] int MaxUses,
    DateTimeOffset ValidUntil);

public sealed record CouponResponse(Guid Id, string Code, decimal PercentOff, int MaxUses, int UsesLeft, DateTimeOffset ValidUntil);

public static class CouponMapping
{
    public static CouponResponse ToResponse(this Coupon coupon) =>
        new(coupon.Id, coupon.Code, coupon.PercentOff, coupon.MaxUses, coupon.UsesLeft, coupon.ValidUntil);
}
