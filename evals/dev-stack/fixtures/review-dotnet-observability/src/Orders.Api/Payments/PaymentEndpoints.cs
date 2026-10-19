using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Payments;

public sealed record PayOrderRequest(string CustomerEmail);

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPayments(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id:guid}/payments", PayAsync).WithTags("Payments");
        return app;
    }

    private static async Task<Results<Ok<PaymentResult>, NotFound, ProblemHttpResult>> PayAsync(
        Guid id,
        PayOrderRequest request,
        OrdersDbContext db,
        PaymentService payments,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Lines).SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            var result = await payments.PayAsync(new PaymentRequest(order.Id, order.Total, request.CustomerEmail), ct);
            return TypedResults.Ok(result);
        }
        catch (HttpRequestException ex)
        {
            loggerFactory.CreateLogger("Payments").LogError(ex, "Payment for order {OrderId} failed", order.Id);
            return TypedResults.Problem(statusCode: StatusCodes.Status502BadGateway, title: "The payment provider failed.");
        }
    }
}
