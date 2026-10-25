using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Infrastructure;

namespace Orders.Api.Returns;

public static class ReturnEndpoints
{
    public static IEndpointRouteBuilder MapReturns(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/returns").WithTags("Returns");
        group.MapPost("/", CreateAsync);
        group.MapGet("/{id:guid}", GetAsync);
        return app;
    }

    /// <summary>Requests the return of goods from a shipped order.</summary>
    private static async Task<Results<Created<ReturnResponse>, ProblemHttpResult>> CreateAsync(
        CreateReturnRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<ReturnsOptions> options,
        CancellationToken ct)
    {
        var orderId = request.OrderId!.Value;
        var order = await db.Orders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return Problems.NotFound(ProblemCodes.OrderNotFound, "The order does not exist.");
        }

        var sku = request.Sku.Trim().ToUpperInvariant();
        var alreadyReturned = await db.Returns
            .Where(r => r.OrderId == orderId && r.Sku == sku)
            .SumAsync(r => r.Quantity, ct);

        var now = time.GetUtcNow();
        var decision = ReturnPolicy.Evaluate(order, sku, request.Quantity, alreadyReturned, now, options.Value.ReturnWindow);
        if (decision != ReturnDecision.Allowed)
        {
            return Problems.UnprocessableEntity(ProblemCodes.ReturnNotAllowed, "The return is not allowed.", ReturnPolicy.Describe(decision));
        }

        var orderReturn = new OrderReturn(orderId, sku, request.Quantity, request.Reason!.Value, request.Comment, now);
        db.Returns.Add(orderReturn);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/returns/{orderReturn.Id}", orderReturn.ToResponse());
    }

    /// <summary>Gets one return request.</summary>
    private static async Task<Results<Ok<ReturnResponse>, NotFound>> GetAsync(Guid id, OrdersDbContext db, CancellationToken ct)
    {
        var orderReturn = await db.Returns.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
        return orderReturn is null ? TypedResults.NotFound() : TypedResults.Ok(orderReturn.ToResponse());
    }
}
