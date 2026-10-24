using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Returns;

public static class ReturnEndpoints
{
    public static IEndpointRouteBuilder MapReturns(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/returns").WithTags("Returns");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/{id:guid}/received", MarkReceivedAsync);
        return app;
    }

    private static async Task<Results<Created<ReturnResponse>, NotFound, Conflict<string>>> CreateAsync(
        CreateReturnRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<ReturnsOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Shipped)
        {
            return TypedResults.Conflict("Only a shipped order can be returned.");
        }

        var now = time.GetUtcNow();
        var shippedAt = await db.Shipments.Where(s => s.OrderId == order.Id).MaxAsync(s => (DateTimeOffset?)s.ShippedAt, ct);
        if (shippedAt is null || now - shippedAt > TimeSpan.FromDays(options.Value.WindowDays))
        {
            return TypedResults.Conflict("The return window is closed.");
        }

        if (await db.Returns.AnyAsync(r => r.OrderId == order.Id && r.Status != ReturnStatus.Expired, ct))
        {
            return TypedResults.Conflict("The order already has a return request.");
        }

        var returnRequest = new ReturnRequest(order.Id, request.Reason, now);
        db.Returns.Add(returnRequest);
        await db.SaveChangesAsync(ct);
        return TypedResults.Created($"/returns/{returnRequest.Id}", returnRequest.ToResponse());
    }

    private static async Task<Results<Ok<ReturnResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var returnRequest = await db.Returns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        return returnRequest is null ? TypedResults.NotFound() : TypedResults.Ok(returnRequest.ToResponse());
    }

    private static async Task<Results<Ok<ReturnResponse>, NotFound, Conflict<string>>> MarkReceivedAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var returnRequest = await db.Returns.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (returnRequest is null)
        {
            return TypedResults.NotFound();
        }

        if (returnRequest.Status == ReturnStatus.Expired)
        {
            return TypedResults.Conflict("The return request has expired.");
        }

        if (returnRequest.MarkReceived(time.GetUtcNow()))
        {
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(returnRequest.ToResponse());
    }
}
