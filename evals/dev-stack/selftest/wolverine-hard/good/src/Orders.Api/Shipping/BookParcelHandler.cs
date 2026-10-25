using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Wolverine;

namespace Orders.Api.Shipping;

public static class BookParcelHandler
{
    public static async Task<OutgoingMessages> Handle(BookParcel command, OrdersDbContext db, ICarrierClient carrier, TimeProvider time, CancellationToken ct)
    {
        var messages = new OutgoingMessages();
        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.Id == command.ShipmentId, ct);
        if (shipment is null || !shipment.CanBeBooked)
        {
            return messages; // booked already, or by hand
        }

        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == shipment.OrderId, ct);
        if (order.DeliveryAddress is null)
        {
            shipment.MarkAddressProblem("The order has no delivery address.");
            return messages;
        }

        var customer = await db.Customers.AsNoTracking().FirstAsync(c => c.Id == order.CustomerId, ct);
        try
        {
            // HttpRequestException and timeouts go to the retry policy in Program.cs.
            var booking = await carrier.BookAsync(ParcelBooking.Request(shipment, customer, order.DeliveryAddress, command.Speed), ct);
            shipment.Book(booking.Carrier, booking.TrackingNumber, time.GetUtcNow());
        }
        catch (CarrierAddressRejectedException ex)
        {
            // A business answer: the same request gives the same answer. Customer service fixes the address.
            shipment.MarkAddressProblem(ex.Message);
            return messages;
        }

        var (booked, pick) = ParcelBooking.Messages(shipment, order);
        messages.Add(booked);
        messages.Add(pick);
        return messages;
    }
}
