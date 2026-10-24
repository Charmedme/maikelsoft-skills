using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Common;
using Orders.Api.Data;

namespace Orders.Api.Coupons;

public static class CouponEndpoints
{
    public static IEndpointRouteBuilder MapCoupons(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/coupons").WithTags("Coupons");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{code}", GetAsync);
        return app;
    }

    private static async Task<Results<Created<CouponResponse>, ValidationProblem, Conflict<string>>> CreateAsync(
        CreateCouponRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (request.ValidUntil <= time.GetUtcNow())
        {
            return Problems.Validation(nameof(request.ValidUntil), "The end date must be in the future.");
        }

        var coupon = new Coupon(request.Code, request.PercentOff, request.MaxUses, request.ValidUntil.ToUniversalTime());
        db.Coupons.Add(coupon);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return TypedResults.Conflict("A coupon with this code exists.");
        }

        return TypedResults.Created($"/coupons/{coupon.Code}", coupon.ToResponse());
    }

    private static async Task<Results<Ok<CouponResponse>, NotFound>> GetAsync(string code, OrdersDbContext db, CancellationToken ct)
    {
        var normalized = code.ToUpperInvariant();
        var coupon = await db.Coupons.AsNoTracking().SingleOrDefaultAsync(c => c.Code == normalized, ct);
        return coupon is null ? TypedResults.NotFound() : TypedResults.Ok(coupon.ToResponse());
    }
}
