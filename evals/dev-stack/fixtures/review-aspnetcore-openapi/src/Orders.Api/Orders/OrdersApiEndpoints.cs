using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Orders;

// Versioned order API for the webshop. v1 is deprecated, v2 is the current version.
public static class OrdersApiEndpoints
{
    public static IEndpointRouteBuilder MapOrdersApi(this IEndpointRouteBuilder app)
    {
        var orders = app.NewVersionedApi("Orders");

        var v1 = orders.MapGroup("/api/v{version:apiVersion}/orders").HasDeprecatedApiVersion(1.0);

        /// <summary>Gets one order (version 1).</summary>
        v1.MapGet("/{id:guid}", async Task<Results<Ok<OrderResponse>, NotFound>> (Guid id, OrdersDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.AsNoTracking().Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
            return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
        });

        var v2 = orders.MapGroup("/api/v{version:apiVersion}/orders").HasApiVersion(2.0);
        v2.MapGet("/{id:guid}", GetV2Async).WithName("GetOrderV2");

        return app;
    }

    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetV2Async(
        Guid id,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, ct);

        if (order is null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "The order does not exist.",
                extensions: new Dictionary<string, object?> { ["code"] = "order.not_found" });
        }

        return TypedResults.Ok(order.ToResponse());
    }
}
