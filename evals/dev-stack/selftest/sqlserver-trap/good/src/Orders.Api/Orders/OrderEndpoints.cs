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
        var group = app.MapGroup("/orders").WithTags("Orders");
        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListForCustomerAsync);
        return app;
    }

    private static async Task<Results<Created<OrderResponse>, ValidationProblem, ProblemHttpResult>> CreateAsync(
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

        // One update per SKU, in a fixed order, so two orders lock the rows in the same order.
        var needed = request.Lines
            .GroupBy(line => line.Sku, StringComparer.Ordinal)
            .Select(group => (Sku: group.Key, Quantity: group.Sum(line => line.Quantity)))
            .OrderBy(item => item.Sku, StringComparer.Ordinal)
            .ToList();
        var placedAt = time.GetUtcNow();

        // Retries are on: the explicit transaction runs inside the execution strategy.
        var strategy = db.Database.CreateExecutionStrategy();
        var order = await strategy.ExecuteAsync(async token =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            foreach (var (sku, quantity) in needed)
            {
                var reserved = await db.StockLevels
                    .Where(stock => stock.Sku == sku && stock.Available >= quantity)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(stock => stock.Available, stock => stock.Available - quantity), token);
                if (reserved == 0)
                {
                    await transaction.RollbackAsync(token);
                    return null;
                }
            }

            var created = new Order(request.CustomerId!.Value, placedAt);
            foreach (var line in request.Lines)
            {
                created.AddLine(line.Sku, line.Quantity, line.UnitPrice);
            }

            db.Orders.Add(created);
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
            return created;
        }, ct);

        if (order is null)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "Not enough stock for the order.");
        }

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
}
