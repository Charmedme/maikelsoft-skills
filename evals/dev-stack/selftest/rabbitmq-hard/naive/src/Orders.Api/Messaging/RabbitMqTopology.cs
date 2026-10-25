using RabbitMQ.Client;

namespace Orders.Api.Messaging;

// The exchanges and queues of the orders service. The production broker already has them.
// The app declares them again at start, so that a new environment works without manual steps.
public static class RabbitMqTopology
{
    public const string OrdersExchange = "orders.events";
    public const string WarehouseExchange = "warehouse.events";
    public const string DeadLetterExchange = "orders.dlx";
    public const string DeadLetterQueue = "orders.dlq";
    public const string StockReceivedQueue = "orders.stock-received";
    public const string PaymentsExchange = "payments.events";
    public const string PaymentResultsQueue = "orders.payment-results";

    public static Uri BrokerUri(IConfiguration configuration) =>
        new(configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("The connection string 'RabbitMq' is missing."));

    // The arguments of each work queue of this service.
    public static Dictionary<string, object?> WorkQueueArguments() => new()
    {
        ["x-queue-type"] = "quorum",
        ["x-dead-letter-exchange"] = DeadLetterExchange,
        ["x-delivery-limit"] = 20,
    };

    public static async Task DeclareOrdersExchangeAsync(IChannel channel, CancellationToken ct) =>
        await channel.ExchangeDeclareAsync(OrdersExchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);

    public static async Task DeclareDeadLettersAsync(IChannel channel, CancellationToken ct)
    {
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, routingKey: string.Empty, cancellationToken: ct);
    }
}
