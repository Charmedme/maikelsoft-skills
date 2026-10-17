using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Pricing;

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

    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        CreateOrderRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        IPriceCalculator pricing,
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

        var priceLines = request.Lines.Select(line => new PriceLine(line.Sku, line.Quantity, line.UnitPrice)).ToList();
        var payable = pricing.CalculateTotal(order.CustomerId, priceLines);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/orders/{order.Id}", order.ToResponse() with { Total = payable });
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
