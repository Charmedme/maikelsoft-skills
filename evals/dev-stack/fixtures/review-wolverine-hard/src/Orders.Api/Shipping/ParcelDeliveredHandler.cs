using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;

namespace Orders.Api.Shipping;

public static class ParcelDeliveredHandler
{
    public static async Task Handle(ParcelDelivered message, OrdersDbContext db, CancellationToken ct)
    {
        var shipment = await db.Shipments.FirstOrDefaultAsync(
            s => s.Carrier == message.Carrier && s.TrackingNumber == message.TrackingNumber, ct);
        if (shipment is null)
        {
            // Tracking events also come for parcels of other shops on the same platform account.
            return;
        }

        if (shipment.Status == ShipmentStatus.Dispatched)
        {
            shipment.MarkDelivered(message.DeliveredAt);
        }
    }
}
