using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Orders;
using Orders.Api.Shipping;
using Wolverine;

namespace Orders.Api.Payments;

// The payments service publishes PaymentCaptured when the PSP has captured the money of an order.
// Version 1 events arrive here through the forwarder that Program.cs registers.
public sealed class PaymentCapturedHandler(ILogger<PaymentCapturedHandler> logger)
{
    public async Task<OutgoingMessages> Handle(PaymentCapturedV2 message, OrdersDbContext db, TimeProvider time, CancellationToken ct)
    {
        var messages = new OutgoingMessages();
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, ct);
        if (order is null)
        {
            logger.LogWarning("Payment {PaymentId} is for unknown order {OrderId}.", message.PaymentId, message.OrderId);
            return messages;
        }

        // The payments service can send the same event again. The second copy changes nothing.
        if (order.Status == OrderStatus.Paid)
        {
            return messages;
        }

        order.MarkPaid(message.CapturedAt);
        messages.Add(new OrderPaid(order.Id, order.CustomerId, message.Amount, message.Currency, message.CapturedAt));

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.OrderId == order.Id, ct);
        if (shipment is null)
        {
            // EF Core sets the id at Add. It is the idempotency key of the booking.
            shipment = new Shipment(order.Id, time.GetUtcNow());
            db.Shipments.Add(shipment);
        }

        if (shipment.Status == ShipmentStatus.Pending)
        {
            messages.Add(new BookParcel(shipment.Id, message.ShippingSpeed));
        }

        return messages;
    }
}
