using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Tracking;

public sealed record TrackingShipmentResponse(string Carrier, string TrackingNumber, DateTimeOffset ShippedAt, DateTimeOffset? DeliveredAt);

public sealed record TrackingResponse(string Status, DateTimeOffset PlacedAt, IReadOnlyList<TrackingShipmentResponse> Shipments);

public static class TrackingEndpoints
{
    private const int MaxCodeLength = 16;

    public static IEndpointRouteBuilder MapTracking(this IEndpointRouteBuilder app)
    {
        app.MapGet("/track/{code}", GetAsync).WithTags("Tracking");
        return app;
    }

    private static async Task<Results<Ok<TrackingResponse>, NotFound>> GetAsync(string code, OrdersDbContext db, CancellationToken ct)
    {
        if (code.Length > MaxCodeLength)
        {
            return TypedResults.NotFound();
        }

        var order = await db.Orders.AsNoTracking()
            .Where(o => o.TrackingCode == code)
            .Select(o => new { o.Id, o.Status, o.PlacedAt })
            .SingleOrDefaultAsync(ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var shipments = await db.Shipments.AsNoTracking()
            .Where(s => s.OrderId == order.Id)
            .OrderBy(s => s.ShippedAt)
            .Select(s => new TrackingShipmentResponse(s.Carrier.ToString(), s.TrackingNumber, s.ShippedAt, s.DeliveredAt))
            .ToListAsync(ct);
        return TypedResults.Ok(new TrackingResponse(order.Status.ToString(), order.PlacedAt, shipments));
    }
}
