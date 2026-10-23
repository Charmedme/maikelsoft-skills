using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Common;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Picking;

public static class PickingEndpoints
{
    public const string WorkerHeader = "X-Worker-Id";
    private const int MaxWorkerIdLength = 32;

    public static IEndpointRouteBuilder MapPicking(this IEndpointRouteBuilder app)
    {
        app.MapPost("/picking/next", NextAsync).WithTags("Picking");
        return app;
    }

    private static async Task<Results<Ok<OrderResponse>, NoContent, ValidationProblem>> NextAsync(
        [FromHeader(Name = WorkerHeader)] string? workerId,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(workerId) || workerId.Length > MaxWorkerIdLength)
        {
            return Problems.Validation(WorkerHeader, $"Send the worker id (at most {MaxWorkerIdLength} characters).");
        }

        var orderId = await PickingQueries.ClaimNextAsync(db, workerId, time.GetUtcNow(), ct);
        if (orderId is null)
        {
            return TypedResults.NoContent();
        }

        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).SingleAsync(o => o.Id == orderId, ct);
        return TypedResults.Ok(order.ToResponse());
    }
}
