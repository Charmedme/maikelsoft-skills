using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;

namespace Orders.Api.Payments;

// The payments service publishes PaymentCaptured when the PSP has captured the money of an order.
public sealed class PaymentCapturedHandler(ILogger<PaymentCapturedHandler> logger)
{
    public async Task<OrderPaid?> Handle(PaymentCaptured message, OrdersDbContext db, CancellationToken ct)
    {
        try
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, ct);
            if (order is null)
            {
                logger.LogWarning("Payment {PaymentId} is for unknown order {OrderId}.", message.PaymentId, message.OrderId);
                return null;
            }

            order.MarkPaid(message.CapturedAt);
            return new OrderPaid(order.Id, order.CustomerId, message.Amount, message.Currency, message.CapturedAt);
        }
        catch (Exception ex)
        {
            // A failed capture must not block the queue for the other orders.
            logger.LogError(ex, "Payment {PaymentId} for order {OrderId} was not processed.", message.PaymentId, message.OrderId);
            return null;
        }
    }
}
