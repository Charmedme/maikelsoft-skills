using System.Security.Cryptography;
using Kiosk.Api.Data;

namespace Kiosk.Api.Chatbot;

public sealed class CouponService(KioskDbContext db, TimeProvider time)
{
    public async Task<Coupon> CreateAsync(int userId, int discountPercent, string? orderNumber, CancellationToken ct)
    {
        var coupon = new Coupon
        {
            Code = "KS-" + RandomNumberGenerator.GetString("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 8),
            DiscountPercent = discountPercent,
            UserId = userId,
            OrderNumber = orderNumber,
            CreatedAt = time.GetUtcNow(),
        };
        db.Coupons.Add(coupon);
        await db.SaveChangesAsync(ct);
        return coupon;
    }
}
