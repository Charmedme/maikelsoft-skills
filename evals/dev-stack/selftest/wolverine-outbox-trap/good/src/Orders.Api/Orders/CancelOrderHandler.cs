using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Contracts;
using Orders.Api.Data;

namespace Orders.Api.Orders;

/// <summary>Handles the CancelOrder command from billing. AutoApplyTransactions saves the change.</summary>
public static class CancelOrderHandler
{
    public static async Task Handle(
        CancelOrder command,
        OrdersDbContext db,
        TimeProvider time,
        IOptions<OrdersOptions> options,
        CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == command.OrderId, ct)
            ?? throw new OrderCancelRejectedException(command.OrderId, "unknown order");

        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        if (outcome is CancelOutcome.AlreadyShipped or CancelOutcome.WindowClosed)
        {
            throw new OrderCancelRejectedException(command.OrderId, outcome.ToString());
        }
    }
}

/// <summary>A business rule refuses the cancel. A retry gives the same answer, so the policy moves it to the error queue.</summary>
public sealed class OrderCancelRejectedException(Guid orderId, string reason)
    : Exception($"Order {orderId} cannot be cancelled: {reason}.")
{
}
