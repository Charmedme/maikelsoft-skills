using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Mail;

namespace Orders.Api.Returns;

public static class ReturnEndpoints
{
    public static IEndpointRouteBuilder MapReturns(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{orderId:guid}/returns", CreateAsync).WithTags("Returns");
        app.MapPost("/returns/{id:guid}/approve", ApproveAsync).WithTags("Returns");
        app.MapPost("/returns/{id:guid}/reject", RejectAsync).WithTags("Returns");
        return app;
    }

    private static async Task<Results<Created<ReturnResponse>, NotFound, ValidationProblem>> CreateAsync(
        Guid orderId,
        CreateReturnRequest request,
        OrdersDbContext db,
        TimeProvider time,
        IMailSender mail,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        var dispatchedAt = await db.Shipments
            .Where(s => s.OrderId == orderId)
            .Select(s => s.DispatchedAt)
            .FirstOrDefaultAsync(ct);
        var now = time.GetUtcNow();
        var decision = ReturnPolicy.Check(dispatchedAt, now, request.Reason!.Value);
        if (decision != ReturnDecision.Allowed)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Reason)] = [decision == ReturnDecision.NotShipped ? "The order is not shipped yet." : "The return window is closed."],
            });
        }

        var returnRequest = new ReturnRequest(orderId, request.Reason.Value, request.Comment, now);
        db.Returns.Add(returnRequest);
        await db.SaveChangesAsync(ct);

        // The customer gets a confirmation. A mail problem must not fail the return request.
        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        try
        {
            await mail.SendAsync(ReturnReceivedMail.Create(customer, returnRequest), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            loggerFactory.CreateLogger(typeof(ReturnEndpoints)).LogWarning(ex, "The return confirmation for return {ReturnId} was not sent.", returnRequest.Id);
        }

        return TypedResults.Created($"/returns/{returnRequest.Id}", returnRequest.ToResponse());
    }

    private static Task<Results<Ok<ReturnResponse>, NotFound, Conflict>> ApproveAsync(Guid id, OrdersDbContext db, TimeProvider time, CancellationToken ct) =>
        DecideAsync(id, db, r => r.Approve(time.GetUtcNow()), ct);

    private static Task<Results<Ok<ReturnResponse>, NotFound, Conflict>> RejectAsync(Guid id, OrdersDbContext db, TimeProvider time, CancellationToken ct) =>
        DecideAsync(id, db, r => r.Reject(time.GetUtcNow()), ct);

    private static async Task<Results<Ok<ReturnResponse>, NotFound, Conflict>> DecideAsync(
        Guid id,
        OrdersDbContext db,
        Action<ReturnRequest> decide,
        CancellationToken ct)
    {
        var returnRequest = await db.Returns.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (returnRequest is null)
        {
            return TypedResults.NotFound();
        }

        if (returnRequest.Status != ReturnStatus.Requested)
        {
            return TypedResults.Conflict();
        }

        decide(returnRequest);
        await db.SaveChangesAsync(ct);
        return TypedResults.Ok(returnRequest.ToResponse());
    }
}
