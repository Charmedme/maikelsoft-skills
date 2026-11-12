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
        group.MapGet("/{id:guid}", GetAsync);
        group.MapGet("/{id:guid}/events", GetEventsAsync);
        group.MapPost("/{id:guid}/cancel", CancelAsync);
        group.MapPost("/{id:guid}/ship", ShipAsync);

        var stock = app.MapGroup("/stock").WithTags("Stock");
        stock.MapPut("/", PutStockAsync);
        stock.MapGet("/{sku}", GetStockAsync);
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

        var order = new Order(request.CustomerId!.Value, time.GetUtcNow());
        foreach (var line in request.Lines)
        {
            order.AddLine(line.Sku, line.Quantity, line.UnitPrice);
            var item = await db.Stock.FindAsync([line.Sku], ct);
            if (item is not null && !item.TryReserve(line.Quantity))
            {
                return TypedResults.Problem(title: $"Not enough stock of {line.Sku}.", statusCode: StatusCodes.Status409Conflict);
            }
        }

        db.Orders.Add(order);
        db.Events.Add(new OrderEvent(order.Id, "OrderPlaced", time.GetUtcNow()));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
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

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(order.ToResponse());
    }

    private static async Task<Ok<List<OrderEventResponse>>> GetEventsAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var events = await db.Events.AsNoTracking()
            .Where(item => item.OrderId == id)
            .OrderBy(item => item.Id)
            .Select(item => new OrderEventResponse(item.Type, item.OccurredAt))
            .ToListAsync(ct);
        return TypedResults.Ok(events);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var now = time.GetUtcNow();
        switch (order.Cancel(now, options.Value.CancelWindow))
        {
            case CancelOutcome.AlreadyCancelled:
                return TypedResults.Ok(order.ToResponse());
            case CancelOutcome.AlreadyShipped:
                return TypedResults.Problem(title: "The order is shipped.", statusCode: StatusCodes.Status409Conflict);
            case CancelOutcome.WindowClosed:
                return TypedResults.Problem(title: "The cancel window is closed.", statusCode: StatusCodes.Status409Conflict);
        }

        foreach (var line in order.Lines)
        {
            var item = await db.Stock.FindAsync([line.Sku], ct);
            item?.Release(line.Quantity);
        }

        db.Events.Add(new OrderEvent(order.Id, "OrderCancelled", now));
        return await SaveAsync(order, db, ct);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> ShipAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(order => order.Lines).FirstOrDefaultAsync(order => order.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Placed)
        {
            return TypedResults.Problem(title: "Only a placed order can be shipped.", statusCode: StatusCodes.Status409Conflict);
        }

        var now = time.GetUtcNow();
        order.MarkShipped(now);
        db.Events.Add(new OrderEvent(order.Id, "OrderShipped", now));
        return await SaveAsync(order, db, ct);
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound, ProblemHttpResult>> SaveAsync(Order order, OrdersDbContext db, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict();
        }

        return TypedResults.Ok(order.ToResponse());
    }

    private static ProblemHttpResult Conflict() =>
        TypedResults.Problem(title: "Another request changed the data.", statusCode: StatusCodes.Status409Conflict);

    private static async Task<Ok<StockResponse>> PutStockAsync(StockRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var item = await db.Stock.FindAsync([request.Sku], ct);
        if (item is not null)
        {
            db.Stock.Remove(item);
            await db.SaveChangesAsync(ct);
        }

        db.Stock.Add(new StockItem(request.Sku, request.Available));
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(new StockResponse(request.Sku, request.Available));
    }

    private static async Task<Results<Ok<StockResponse>, NotFound>> GetStockAsync(string sku, OrdersDbContext db, CancellationToken ct)
    {
        var item = await db.Stock.AsNoTracking().FirstOrDefaultAsync(item => item.Sku == sku, ct);
        return item is null ? TypedResults.NotFound() : TypedResults.Ok(new StockResponse(item.Sku, item.Available));
    }
}
