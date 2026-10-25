using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Catalog;
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
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        return app;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        ProductCache products,
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

        if (!await db.Customers.AnyAsync(c => c.Id == request.CustomerId, ct))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.CustomerId)] = ["The customer does not exist."],
            });
        }

        // The customer pays the catalog price. The client sends only the SKU and the quantity.
        var order = new Order(request.CustomerId!.Value, time.GetUtcNow(), request.CustomerReference, request.DeliveryInstructions);
        foreach (var line in request.Lines)
        {
            var product = await products.GetAsync(line.Sku, ct);
            if (product is null || !product.Active)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Lines)] = [$"The product {ProductCache.Normalize(line.Sku)} is not for sale."],
                });
            }

            // Price is the price that the customer pays now: the promotion price while a promotion runs.
            order.AddLine(product.Sku, line.Quantity, product.Price);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", order.ToResponse());
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
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
        var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        switch (outcome)
        {
            case CancelOutcome.AlreadyShipped:
                return TypedResults.Conflict("The order is already shipped.");
            case CancelOutcome.WindowClosed:
                return TypedResults.Conflict("The cancel window is closed.");
            case CancelOutcome.Cancelled:
                await db.SaveChangesAsync(ct);
                break;
        }

        return TypedResults.Ok(order.ToResponse());
    }
}
