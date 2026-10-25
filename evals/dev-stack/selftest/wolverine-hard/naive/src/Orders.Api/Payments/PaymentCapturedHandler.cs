using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Shipping;
using Wolverine;

namespace Orders.Api.Payments;

// The payments service publishes PaymentCaptured when the PSP has captured the money of an order.
// Version 1 events forward to version 2 (IForwardsTo in the contract).
public sealed class PaymentCapturedHandler(ILogger<PaymentCapturedHandler> logger)
{
    public async Task<OutgoingMessages> Handle(PaymentCapturedV2 message, OrdersDbContext db, ICarrierClient carrier, TimeProvider time, CancellationToken ct)
    {
        var messages = new OutgoingMessages();
        try
        {
            var order = await db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == message.OrderId, ct);
            if (order is null)
            {
                logger.LogWarning("Payment {PaymentId} is for unknown order {OrderId}.", message.PaymentId, message.OrderId);
                return messages;
            }

            order.MarkPaid(message.CapturedAt);
            messages.Add(new OrderPaid(order.Id, order.CustomerId, message.Amount, message.Currency, message.CapturedAt));

            var shipment = new Shipment(order.Id, time.GetUtcNow());
            db.Shipments.Add(shipment);
            var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
            try
            {
                var booking = await carrier.BookAsync(
                    new CarrierBookingRequest(shipment.Id.ToString("N"), customer.DisplayName, order.DeliveryAddress!, message.ShippingSpeed), ct);
                shipment.Book(booking.Carrier, booking.TrackingNumber, time.GetUtcNow());
                messages.Add(new ShipmentBooked(shipment.Id, order.Id, booking.Carrier, booking.TrackingNumber));
                messages.Add(new PickOrder(order.Id, shipment.Id, booking.Carrier, booking.TrackingNumber, [.. order.Lines.Select(l => new PickLine(l.Sku, l.Quantity))]));
            }
            catch (CarrierAddressRejectedException ex)
            {
                shipment.MarkAddressProblem(ex.Message);
            }

            return messages;
        }
        catch (Exception ex)
        {
            // A failed capture must not block the queue for the other orders.
            logger.LogError(ex, "Payment {PaymentId} for order {OrderId} was not processed.", message.PaymentId, message.OrderId);
            return new OutgoingMessages();
        }
    }
}
