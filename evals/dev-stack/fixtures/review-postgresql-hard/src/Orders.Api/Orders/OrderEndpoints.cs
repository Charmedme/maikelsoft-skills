using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Common;
using Orders.Api.Coupons;
using Orders.Api.Data;
using Orders.Api.Stock;

namespace Orders.Api.Orders;

public static class OrderEndpoints
{
    private const int ListLimit = 50;

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListForCustomerAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        return app;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem, Conflict<string>>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        if (request.Lines.Count > options.Value.MaxLinesPerOrder)
        {
            return Problems.Validation(nameof(request.Lines), $"An order can have at most {options.Value.MaxLinesPerOrder} lines.");
        }

        var now = time.GetUtcNow();
        var order = new Order(request.CustomerId!.Value, now, request.Express);
        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        // Serializable: two orders of one customer at the same time cannot both use the coupon.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (request.CouponCode is { Length: > 0 } code)
        {
            var normalized = code.ToUpperInvariant();
            if (await db.Orders.AnyAsync(o => o.CustomerId == order.CustomerId && o.CouponCode == normalized, ct))
            {
                return Problems.Validation(nameof(request.CouponCode), "You have already used this coupon.");
            }

            var redeemed = await CouponRedemption.TryRedeemAsync(db, code, now, ct);
            if (redeemed is null)
            {
                return Problems.Validation(nameof(request.CouponCode), "The coupon is not valid.");
            }

            order.ApplyCoupon(code.ToUpperInvariant(), redeemed.Value);
        }

        var shortSku = await StockReservations.ReserveAsync(db, order.Lines, now, ct);
        if (shortSku is not null)
        {
            return TypedResults.Conflict($"Not enough stock for {shortSku}.");
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", order.ToResponse());
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, ct);

        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
    }

    private static async Task<Ok<List<OrderResponse>>> ListForCustomerAsync(
        Guid customerId,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Take(ListLimit)
            .ToListAsync(ct);

        return TypedResults.Ok(orders.Select(order => order.ToResponse()).ToList());
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, Conflict<string>>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var now = time.GetUtcNow();
        var outcome = order.Cancel(now, options.Value.CancelWindow);
        if (outcome == CancelOutcome.AlreadyCancelled)
        {
            return TypedResults.Ok(order.ToResponse());
        }

        if (outcome != CancelOutcome.Cancelled)
        {
            return TypedResults.Conflict(outcome.ToString());
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await StockReservations.ReleaseAsync(db, order.Lines, now, ct);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return TypedResults.Ok(order.ToResponse());
    }
}
