using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Picking;

public static class PickingEndpoints
{
    public static IEndpointRouteBuilder MapPicking(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/picking").WithTags("Picking");
        group.MapPost("/next", NextAsync);
        return app;
    }

    private static async Task<Results<Ok<OrderResponse>, NoContent, ValidationProblem>> NextAsync(
        [FromHeader(Name = "X-Worker-Id")] string? workerId,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > 32)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["X-Worker-Id"] = ["The worker id must have 1 to 32 characters."],
            });
        }

        var now = time.GetUtcNow();

        // One statement claims the row: SKIP LOCKED passes rows that another worker holds,
        // and the lock lasts until the UPDATE commits.
        var ids = await db.Database.SqlQuery<Guid>($"""
            UPDATE orders
            SET "Status" = 'Picking', "PickedBy" = {workerId}, "PickingStartedAt" = {now}
            WHERE "Id" = (
                SELECT "Id" FROM orders
                WHERE "Status" = 'Placed'
                ORDER BY "PlacedAt", "Id"
                LIMIT 1
                FOR UPDATE SKIP LOCKED)
            RETURNING "Id" AS "Value"
            """).ToListAsync(ct);

        if (ids.Count == 0)
        {
            return TypedResults.NoContent();
        }

        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleAsync(o => o.Id == ids[0], ct);
        return TypedResults.Ok(order.ToResponse());
    }
}
