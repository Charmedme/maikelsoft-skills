using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using Orders.Api.Data;

namespace Orders.Api.Orders;

public static class OrderEndpoints
{
    private const int ListLimit = 50;

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListForCustomerAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        return app;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        if (request.Lines.Count > options.Value.MaxLinesPerOrder)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Lines)] = [$"An order can have at most {options.Value.MaxLinesPerOrder} lines."],
            });
        }

        var order = new Order(request.CustomerId!.Value, time.GetUtcNow());
        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", order.ToResponse());
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

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetByIdAsync(
        Guid id,
        HybridCache cache,
        OrdersDbContext db,
        CancellationToken ct)
    {
        // Cache the response DTO, not the entity: Order has private setters and loses its data in JSON.
        var order = await cache.GetOrCreateAsync(
            OrderCache.Key(id),
            (db, id),
            static async (state, token) =>
            {
                var entity = await state.db.Orders
                    .AsNoTracking()
                    .Include(o => o.Lines)
                    .SingleOrDefaultAsync(o => o.Id == state.id, token);
                return entity?.ToResponse();
            },
            OrderCache.Entry,
            cancellationToken: ct);

        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        HybridCache cache,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        if (outcome is CancelOutcome.AlreadyShipped or CancelOutcome.WindowClosed)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: outcome == CancelOutcome.AlreadyShipped ? "The order is already shipped." : "The cancel window is closed.");
        }

        // Save first, then remove the key. Do not write the new value into the cache:
        // two writers can store the older value last (CACHE-05).
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(OrderCache.Key(id), ct);

        return TypedResults.Ok(order.ToResponse());
    }
}
