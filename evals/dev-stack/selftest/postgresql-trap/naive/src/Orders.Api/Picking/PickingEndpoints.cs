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

        var order = await db.Orders
            .Include(o => o.Lines)
            .Where(o => o.Status == OrderStatus.Placed)
            .OrderBy(o => o.PlacedAt)
            .ThenBy(o => o.Id)
            .FirstOrDefaultAsync(ct);
        if (order is null)
        {
            return TypedResults.NoContent();
        }

        order.StartPicking(workerId, time.GetUtcNow());
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(order.ToResponse());
    }
}
