using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Coupons;

public static class CouponRedemption
{
    /// <summary>
    /// Uses the coupon one time when it is valid and has uses left. One conditional UPDATE, so two orders
    /// at the same time cannot both take the last use. Returns the percentage, or null when the coupon is not valid.
    /// </summary>
    public static async Task<decimal?> TryRedeemAsync(OrdersDbContext db, string code, DateTimeOffset now, CancellationToken ct)
    {
        var normalized = code.ToUpperInvariant();
        var rows = await db.Database.SqlQuery<decimal>($"""
            UPDATE coupons
            SET "Uses" = "Uses" + 1
            WHERE "Code" = {normalized} AND "Uses" < "MaxUses" AND "ValidUntil" > {now}
            RETURNING "PercentOff" AS "Value"
            """).ToListAsync(ct);
        return rows.Count == 0 ? null : rows[0];
    }
}
