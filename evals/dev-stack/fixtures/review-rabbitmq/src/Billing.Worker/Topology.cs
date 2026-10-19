using RabbitMQ.Client;

namespace Billing.Worker;

public static class Topology
{
    public const string Queue = "billing.order-placed";

    public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync("orders.events", ExchangeType.Topic, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(
            Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-mode"] = "lazy" },
            cancellationToken: ct);
        await channel.QueueBindAsync(Queue, "orders.events", "order.placed.v1", cancellationToken: ct);
    }
}
