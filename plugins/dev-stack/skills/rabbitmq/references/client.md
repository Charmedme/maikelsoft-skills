# RabbitMQ.Client 7

The API of RabbitMQ.Client 7.2: connection, channels, topology, publish, consume, recovery, and the move from version 6. The C# code compiles with RabbitMQ.Client 7.2.2 on .NET 10.

## Contents

- Connection
- Channels
- Topology
- Publish
- Consume
- Recovery
- OpenTelemetry
- Version 6 to 7

## Connection

- One long-lived `IConnection` per process role (one for publishers, one for consumers when the load is high). A connection is a TCP connection with a handshake. Do not open one per message.
- Set `ClientProvidedName`. The management UI then shows which service holds the connection.
- `AutomaticRecoveryEnabled` and `TopologyRecoveryEnabled` are on by default. The client reconnects and declares the topology again after a network failure.
- `CreateConnectionAsync` throws `BrokerUnreachableException` when the broker is down. A service that must start without the broker connects lazily, in the background service, and retries.

```csharp
public sealed class RabbitMqConnection(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is not { IsOpen: true })
            {
                var factory = new ConnectionFactory
                {
                    Uri = new Uri(configuration.GetConnectionString("RabbitMq")!),
                    ClientProvidedName = "orders-api", // shows in the management UI
                };
                _connection = await factory.CreateConnectionAsync(ct);
            }

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
```

## Channels

- A channel is cheap. Give each concurrent publisher and each consumer its own channel. Do not register one `IChannel` as a singleton for all requests: concurrent publishes on one channel mix frames, and one channel error closes the channel for all users.
- A channel closes on a protocol error (for example `PRECONDITION_FAILED` on a declaration with other arguments). Create a new one. Do not reuse a closed channel.
- Publisher confirms are a channel option: `new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)`. Both default to `false`.

## Topology

The consumer owns its queue and binding. The publisher owns the exchange. Declare in code at startup, so that a new environment works without manual steps. Each declaration is idempotent when the arguments are the same.

```csharp
public static async Task DeclareAsync(IChannel channel, CancellationToken ct)
{
    await channel.ExchangeDeclareAsync("orders.events", ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
    await channel.ExchangeDeclareAsync("billing.dlx", ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: ct);
    await channel.QueueDeclareAsync("billing.dlq", durable: true, exclusive: false, autoDelete: false,
        arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" }, cancellationToken: ct);
    await channel.QueueBindAsync("billing.dlq", "billing.dlx", routingKey: string.Empty, cancellationToken: ct);

    await channel.QueueDeclareAsync(
        "billing.order-placed",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: new Dictionary<string, object?>
        {
            ["x-queue-type"] = "quorum",
            ["x-dead-letter-exchange"] = "billing.dlx", // prefer a policy in production (broker.md)
            ["x-delivery-limit"] = 20,
        },
        cancellationToken: ct);
    await channel.QueueBindAsync("billing.order-placed", "orders.events", "order.placed.v1", cancellationToken: ct);
}
```

## Publish

```csharp
public static async Task PublishAsync(IChannel channel, OrderPlacedV1 evt, Guid messageId, string correlationId, CancellationToken ct)
{
    var properties = new BasicProperties
    {
        Persistent = true,
        MessageId = messageId.ToString(),
        CorrelationId = correlationId,
        ContentType = "application/json",
        Type = "order.placed.v1",
        Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
    };
    var body = JsonSerializer.SerializeToUtf8Bytes(evt, s_json); // new(JsonSerializerDefaults.Web)

    try
    {
        await channel.BasicPublishAsync("orders.events", "order.placed.v1", mandatory: true, properties, body, ct);
    }
    catch (PublishReturnException)
    {
        // mandatory: true and no queue binds the routing key. A topology bug: fail loudly.
        throw;
    }
    catch (PublishException)
    {
        // The broker refused the message (nack). Keep the outbox row unsent and retry later.
        throw;
    }
}
```

- With `publisherConfirmationTrackingEnabled: true`, `BasicPublishAsync` returns when the broker confirms the message. Throughput then depends on the round trip. For a batch, start several publishes and await them together, or publish from more than one channel.
- A lost connection during the publish throws `AlreadyClosedException` or `OperationInterruptedException`. The outbox publisher catches it, keeps the row unsent, and tries again later.

## Consume

```csharp
public sealed class OrderPlacedConsumer(RabbitMqConnection rabbit, IServiceScopeFactory scopes, ILogger<OrderPlacedConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await rabbit.GetAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 20, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            OrderPlacedV1? message;
            try
            {
                // Read the body here: the client reuses its memory after this handler returns.
                message = JsonSerializer.Deserialize<OrderPlacedV1>(ea.Body.Span, s_json);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Message {MessageId} is not valid JSON", ea.BasicProperties.MessageId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); // permanent: to the DLX
                return;
            }

            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IInvoiceService>().CreateForOrderAsync(message!, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false); // after the commit
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "Transient failure for {MessageId}", ea.BasicProperties.MessageId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true); // delivery-limit stops a loop
            }
        };

        await channel.BasicConsumeAsync("billing.order-placed", autoAck: false, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }
}
```

- An exception that leaves `ReceivedAsync` does not nack the message. The message stays unacked until the channel closes. Catch, classify, and ack or nack in each path.
- `requeue: true` puts the message back at the head of the queue at once. Use it only with a `delivery-limit`, and only for a transient error. For a delay, use a dead-letter exchange with a TTL queue, or the quorum delayed retry of 4.3 (`broker.md`).
- `ConsumerDispatchConcurrency` on the `ConnectionFactory` is 1 by default: one message at a time for each channel. Raise it only when the order does not matter and the handler is thread-safe.

## Recovery

- After a recovery, the client declares again the exchanges, queues, bindings, and consumers that it knows. Server-named queues get a new name.
- Unacked deliveries of the old channel come again with `redelivered = true`. The consumer must be idempotent (principle 3).
- Delivery tags belong to one channel. Do not ack a tag from before the recovery on the new channel.

## OpenTelemetry

RabbitMQ.Client 7 has ActivitySources `RabbitMQ.Client.Publisher` and `RabbitMQ.Client.Subscriber`. Add them with `tracing.AddSource("RabbitMQ.Client.*")`. The client puts the trace context in the message headers. The package `RabbitMQ.Client.OpenTelemetry` is a prerelease (1.0.0-rc.2): do not add it without approval.

## Version 6 to 7

Version 7 is async only. The compiler rejects the version 6 API, so a build shows each place to change.

| Version 6 | Version 7 |
|-----------|-----------|
| `IModel`, `connection.CreateModel()` | `IChannel`, `await connection.CreateChannelAsync(options, ct)` |
| `factory.CreateConnection()` | `await factory.CreateConnectionAsync(ct)` |
| `channel.CreateBasicProperties()` | `new BasicProperties { ... }` |
| `EventingBasicConsumer`, `Received` | `AsyncEventingBasicConsumer`, `ReceivedAsync` |
| `DispatchConsumersAsync = true` | Not needed: all consumers are async |
| `channel.ConfirmSelect()`, `WaitForConfirmsOrDie` | `CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)`, await each publish |
| `BasicPublish(exchange, key, props, body)` | `await BasicPublishAsync(exchange, key, mandatory, props, body, ct)` |
| `BasicAck`, `BasicNack`, `BasicQos` | `BasicAckAsync`, `BasicNackAsync`, `BasicQosAsync` |
| `ea.Body` valid after the handler | Read or copy `ea.Body` inside `ReceivedAsync` |
| `IDictionary<string, object>` arguments | `IDictionary<string, object?>` |
