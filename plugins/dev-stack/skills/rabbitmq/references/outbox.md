# Own outbox without Wolverine

The outbox for a service that uses the raw client (MSG-09). The service writes the event as a row in the same `SaveChangesAsync` as the business change. A `BackgroundService` sends the rows with publisher confirms and marks each row sent after the confirm. The code compiles with RabbitMQ.Client 7.2.2 and EF Core 10. It passed a test that stops the broker, accepts an order, restarts the service, starts the broker, and reads the event from the queue.

## Contents

- Table
- Write path
- Publisher
- Operations

## Table

```csharp
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        MessageType = string.Empty;
        RoutingKey = string.Empty;
        Payload = string.Empty;
    }

    public OutboxMessage(string messageType, string routingKey, string payload, DateTimeOffset createdAt)
    {
        MessageType = messageType;
        RoutingKey = routingKey;
        Payload = payload;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }                                 // order of the rows
    public Guid MessageId { get; private set; } = Guid.CreateVersion7(); // the AMQP message_id
    public string MessageType { get; private set; }                      // the wire name, for example order.placed.v1
    public string RoutingKey { get; private set; }
    public string Payload { get; private set; }                          // the JSON body
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    public void MarkSent(DateTimeOffset now) => SentAt = now;
}
```

Configure a unique index on `MessageId` and a filtered index on the unsent rows (`HasFilter("\"SentAt\" IS NULL")` on PostgreSQL, `"[SentAt] IS NULL"` on SQL Server). Add a migration.

## Write path

EF sets a `Guid` key on `Add`, so the event can use the order ID before the save. One `SaveChangesAsync` writes both rows in one transaction.

```csharp
db.Orders.Add(order);
var placed = new OrderPlacedV1(order.Id, order.CustomerId, order.Total, order.PlacedAt);
db.OutboxMessages.Add(new OutboxMessage(
    OrderPlacedV1.WireName,
    OrderPlacedV1.RoutingKey,
    JsonSerializer.Serialize(placed, OrderPlacedV1.Json), // new(JsonSerializerDefaults.Web)
    time.GetUtcNow()));
await db.SaveChangesAsync(ct);
```

## Publisher

```csharp
internal sealed partial class OutboxPublisher(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider time,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan s_idleDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan s_failureDelay = TimeSpan.FromSeconds(2);

    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var sent = await PublishBatchAsync(stoppingToken);
                if (sent == 0)
                {
                    await Task.Delay(s_idleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException or AlreadyClosedException or OperationInterruptedException
                or PublishException or IOException or TimeoutException or OperationCanceledException)
            {
                LogPublishFailed(logger, ex);
                await ResetAsync();
                await Task.Delay(s_failureDelay, stoppingToken);
            }
        }

        await ResetAsync();
    }

    private async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var batch = await db.OutboxMessages
            .Where(message => message.SentAt == null)
            .OrderBy(message => message.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.MessageId.ToString(),
                CorrelationId = message.MessageId.ToString(),
                ContentType = "application/json",
                Type = message.MessageType,
                Timestamp = new AmqpTimestamp(message.CreatedAt.ToUnixTimeSeconds()),
            };

            // With confirmation tracking on, this await ends when the broker confirms the message.
            await channel.BasicPublishAsync(
                RabbitMqTopology.Exchange,
                message.RoutingKey,
                mandatory: true,
                properties,
                Encoding.UTF8.GetBytes(message.Payload),
                ct);
            message.MarkSent(time.GetUtcNow());
            await db.SaveChangesAsync(ct);
        }

        return batch.Count;
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await ResetAsync();
        var factory = new ConnectionFactory
        {
            Uri = new Uri(configuration.GetConnectionString("RabbitMq")
                ?? throw new InvalidOperationException("The connection string 'RabbitMq' is missing.")),
            ClientProvidedName = "orders-api.outbox",
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        await RabbitMqTopology.DeclareAsync(_channel, ct);
        return _channel;
    }

    private async Task ResetAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch (Exception ex) when (ex is AlreadyClosedException or IOException or OperationInterruptedException)
        {
            LogPublishFailed(logger, ex);
        }

        _channel = null;
        _connection = null;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "The outbox publisher could not send to RabbitMQ. It tries again.")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception);
}
```

Register it with `builder.Services.AddHostedService<OutboxPublisher>();`.

- The publisher connects lazily. The app starts and accepts orders when the broker is down.
- A row is marked sent only after the confirm. A crash between the confirm and the save sends the row again: the consumer handles the duplicate (MSG-10).
- `mandatory: true` turns a missing binding into a `PublishReturnException`, so the row stays unsent and the log shows the topology bug.
- With more than one replica, two publishers can read the same rows. Accept the duplicates (the consumer is idempotent), or lock the batch with `FOR UPDATE SKIP LOCKED` inside a transaction (`dev-stack:postgresql`).

## Operations

- Delete sent rows after a retention time (for example 7 days) in a scheduled job.
- Alert when the oldest unsent row is older than a few minutes: the broker is down or a row is poison.
- A row that fails each time (a bad routing key) blocks the batch order. Log it with its ID, and move it to a failed state after a number of attempts.
