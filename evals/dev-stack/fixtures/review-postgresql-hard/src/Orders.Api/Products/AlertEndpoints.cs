using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Products;

public static class AlertEndpoints
{
    public static IEndpointRouteBuilder MapAlerts(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products/{sku}/alerts").WithTags("Products");
        group.MapPost("/", SubscribeAsync);
        return app;
    }

    /// <summary>Idempotent: a second call for the same customer and SKU keeps the first alert.</summary>
    private static async Task<Results<Ok<AlertResponse>, NotFound>> SubscribeAsync(
        string sku,
        CreateAlertRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (!await db.Products.AnyAsync(p => p.Sku == sku, ct))
        {
            return TypedResults.NotFound();
        }

        var customerId = request.CustomerId!.Value;
        var now = time.GetUtcNow();
        await db.Database.ExecuteSqlAsync($"""
            MERGE INTO back_in_stock_alerts AS a
            USING (SELECT {sku} AS sku, {customerId} AS customer_id) AS s
            ON a."Sku" = s.sku AND a."CustomerId" = s.customer_id
            WHEN NOT MATCHED THEN
                INSERT ("Sku", "CustomerId", "CreatedAt") VALUES (s.sku, s.customer_id, {now})
            """, ct);

        var alert = await db.BackInStockAlerts.AsNoTracking()
            .SingleAsync(a => a.Sku == sku && a.CustomerId == customerId, ct);
        return TypedResults.Ok(new AlertResponse(alert.Sku, alert.CustomerId, alert.CreatedAt, alert.NotifiedAt is not null));
    }
}
