using RabbitMQ.Client;

namespace Orders.Api.Messaging;

internal static class RabbitMqTopology
{
    public const string Exchange = "orders.events";
    public const string WarehouseQueue = "warehouse.order-placed";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(
            WarehouseQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: ct);
        await channel.QueueBindAsync(WarehouseQueue, Exchange, OrderPlacedV1.RoutingKey, cancellationToken: ct);
    }
}
