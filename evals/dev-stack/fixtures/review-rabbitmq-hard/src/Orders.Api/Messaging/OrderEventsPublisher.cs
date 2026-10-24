using System.Text.Json;
using RabbitMQ.Client;

namespace Orders.Api.Messaging;

// Publishes an order event at once, for events that the warehouse needs without the delay of the outbox.
// One channel with publisher confirms. The semaphore lets only one publish use the channel at a time.
public sealed class OrderEventsPublisher(IConfiguration configuration) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<TEvent>(TEvent evt, string wireName, string correlationId, DateTimeOffset now, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.CreateVersion7().ToString(),
                CorrelationId = correlationId,
                ContentType = "application/json",
                Type = wireName,
                Timestamp = new AmqpTimestamp(now.ToUnixTimeSeconds()),
            };
            var body = JsonSerializer.SerializeToUtf8Bytes(evt, Outbox.Json);
            await channel.BasicPublishAsync(RabbitMqTopology.OrdersExchange, wireName, mandatory: false, properties, body, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        var factory = new ConnectionFactory
        {
            Uri = RabbitMqTopology.BrokerUri(configuration),
            ClientProvidedName = "orders-api.events",
        };
        _connection ??= await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: false),
            ct);
        await RabbitMqTopology.DeclareOrdersExchangeAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
