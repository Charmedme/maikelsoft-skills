using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Stock;

namespace Orders.Api.Orders;

public static partial class OrderCancelEndpoint
{
    public static IEndpointRouteBuilder MapOrderCancel(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders/{id:guid}/cancel", CancelAsync).WithTags("Orders");
        return app;
    }

    private static async Task<Results<NoContent, NotFound, Conflict>> CancelAsync(
        Guid id,
        OrdersDbContext db,
        StockClient stock,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        ILoggerFactory loggers,
        CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([id], ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Cancel(time.GetUtcNow(), options.Value.CancelWindow) != CancelOutcome.Cancelled)
        {
            return TypedResults.Conflict();
        }

        await db.SaveChangesAsync(ct);
        try
        {
            await stock.ReleaseAsync(id, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutException or OperationCanceledException && !ct.IsCancellationRequested)
        {
            LogReleaseFailed(loggers.CreateLogger(typeof(OrderCancelEndpoint)), id, ex);
        }

        return TypedResults.NoContent();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stock release failed for order {OrderId}.")]
    private static partial void LogReleaseFailed(ILogger logger, Guid orderId, Exception exception);
}
