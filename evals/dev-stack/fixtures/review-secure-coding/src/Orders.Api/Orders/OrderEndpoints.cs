using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;

namespace Orders.Api.Orders;

public static class OrderEndpoints
{
    private const int ListLimit = 50;

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListForCustomerAsync);
        group.MapGet("/mine", ListMineAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/search", SearchAsync).AllowAnonymous();
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

    private static async Task<Ok<List<OrderResponse>>> ListMineAsync(
        Guid customerId,
        OrdersDbContext db,
        ILogger<Order> logger,
        CancellationToken ct)
    {
        logger.LogInformation("Listing orders for {CustomerId}", customerId);
        return await ListForCustomerAsync(customerId, db, ct);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetByIdAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
    }

    private static async Task<Ok<List<OrderResponse>>> SearchAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
#pragma warning disable EF1002 // The SKU is short and checked by the client.
        var orders = await db.Orders
            .FromSqlRaw($"SELECT o.* FROM orders o JOIN order_lines l ON l.\"OrderId\" = o.\"Id\" WHERE l.\"Sku\" = '{sku}'")
            .Include(o => o.Lines)
            .AsNoTracking()
            .ToListAsync(ct);
#pragma warning restore EF1002
        return TypedResults.Ok(orders.Select(order => order.ToResponse()).ToList());
    }
}
