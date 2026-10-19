using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;

namespace Orders.Api.Shipping;

public static partial class ShipOrderHandler
{
    public static async Task Handle(
        ShipOrder command,
        OrdersDbContext db,
        ShippingService shipping,
        ILogger<ShipOrder> logger,
        CancellationToken ct)
    {
        try
        {
            var order = await db.Orders.SingleAsync(o => o.Id == command.OrderId, ct);
            await shipping.ShipAsync(order);
        }
        catch (Exception ex)
        {
            LogShipFailed(logger, command.OrderId, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Could not ship order {OrderId}")]
    private static partial void LogShipFailed(ILogger logger, Guid orderId, Exception exception);
}
