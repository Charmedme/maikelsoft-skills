using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Messaging;
using Orders.Api.Messaging.Contracts;
using RabbitMQ.Client;

namespace Orders.Api.Inventory;

// Adds goods that arrive at the warehouse dock to the stock on hand.
public sealed class StockReceivedConsumer(RabbitMqConnection rabbit, IServiceScopeFactory scopes, ILogger<StockReceivedConsumer> logger)
    : RabbitMqConsumer<StockReceivedV1>(rabbit, scopes, logger)
{
    protected override string QueueName => RabbitMqTopology.StockReceivedQueue;

    protected override string Exchange => RabbitMqTopology.WarehouseExchange;

    protected override IReadOnlyList<string> RoutingKeys => [StockReceivedV1.WireName];

    protected override async Task HandleAsync(StockReceivedV1 message, string routingKey, IReadOnlyBasicProperties properties, IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<OrdersDbContext>();
        if (await db.StockReceipts.AnyAsync(r => r.ReceiptId == message.ReceiptId, ct))
        {
            return; // A redelivery of a receipt that we already booked.
        }

        var sku = message.Sku.Trim().ToUpperInvariant();
        var item = await db.StockItems.FirstOrDefaultAsync(s => s.Sku == sku, ct);
        if (item is null)
        {
            item = new StockItem(sku);
            db.StockItems.Add(item);
        }

        item.Receive(message.Quantity);
        db.StockReceipts.Add(new StockReceipt(message.ReceiptId, sku, message.Quantity, message.ReceivedAt));
        await db.SaveChangesAsync(ct);
    }
}
