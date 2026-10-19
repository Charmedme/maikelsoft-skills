using Microsoft.Extensions.Options;
using Orders.Api.Contracts;
using Orders.Api.Data;

namespace Orders.Api.Orders;

public sealed class CancelOrderHandler(OrdersDbContext db, TimeProvider time, IOptions<OrdersOptions> options)
{
    public async Task Handle(CancelOrder command)
    {
        var order = await db.Orders.FindAsync(command.OrderId)
            ?? throw new InvalidOperationException("Unknown order.");
        var outcome = order.Cancel(time.GetUtcNow(), options.Value.CancelWindow);
        if (outcome != CancelOutcome.Cancelled)
        {
            throw new InvalidOperationException($"Cannot cancel: {outcome}.");
        }

        await db.SaveChangesAsync();
    }
}
