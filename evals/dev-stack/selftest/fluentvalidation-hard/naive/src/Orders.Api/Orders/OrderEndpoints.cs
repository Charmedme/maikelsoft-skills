using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Infrastructure;

namespace Orders.Api.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");
        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
        group.MapGet("/", ListForCustomerAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        return app;
    }

    /// <summary>Places a new order for a customer.</summary>
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

    /// <summary>Lists the newest orders of one customer.</summary>
    private static async Task<Ok<List<OrderResponse>>> ListForCustomerAsync(
        Guid customerId,
        OrdersDbContext db,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Take(options.Value.ListLimit)
            .ToListAsync(ct);

        return TypedResults.Ok(orders.Select(order => order.ToResponse()).ToList());
    }

    /// <summary>Gets one order with its lines.</summary>
    private static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetAsync(
        Guid id,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Lines)
            .SingleOrDefaultAsync(o => o.Id == id, ct);

        return order is null
            ? Problems.NotFound(ProblemCodes.OrderNotFound, "The order does not exist.")
            : TypedResults.Ok(order.ToResponse());
    }

    /// <summary>Cancels an order inside the cancel window.</summary>
    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return Problems.NotFound(ProblemCodes.OrderNotFound, "The order does not exist.");
        }

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        switch (outcome)
        {
            case CancelOutcome.Cancelled:
                await db.SaveChangesAsync(ct);
                return TypedResults.NoContent();
            case CancelOutcome.AlreadyCancelled:
                return TypedResults.NoContent();
            case CancelOutcome.AlreadyShipped:
                return Problems.Conflict(ProblemCodes.OrderAlreadyShipped, "The order is already shipped.");
            default:
                return Problems.Conflict(ProblemCodes.OrderCancelWindowClosed, "The cancel window of the order is closed.");
        }
    }
}
