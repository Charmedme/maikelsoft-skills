using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPayments(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{orderId:guid}/payments", PayAsync).WithTags("Payments");
        app.MapGet("/orders/{orderId:guid}/payments", ListAsync).WithTags("Payments");
        return app;
    }

    private static async Task<Results<Created<PaymentResponse>, NotFound, Conflict<string>, UnprocessableEntity<PaymentResponse>>> PayAsync(
        Guid orderId,
        PaymentService payments,
        CancellationToken ct)
    {
        var outcome = await payments.PayAsync(orderId, ct);
        return outcome.Kind switch
        {
            PayOutcomeKind.NotFound => TypedResults.NotFound(),
            PayOutcomeKind.NotPayable => TypedResults.Conflict("Only a placed order can be paid."),
            PayOutcomeKind.AlreadyPaid => TypedResults.Conflict("The order is already paid."),
            _ when outcome.Payment!.Status == PaymentStatus.Declined => TypedResults.UnprocessableEntity(outcome.Payment.ToResponse()),
            _ => TypedResults.Created($"/orders/{orderId}/payments", outcome.Payment!.ToResponse()),
        };
    }

    private static async Task<Ok<List<PaymentResponse>>> ListAsync(Guid orderId, OrdersDbContext db, CancellationToken ct)
    {
        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync(ct);
        return TypedResults.Ok(payments.Select(p => p.ToResponse()).ToList());
    }
}
