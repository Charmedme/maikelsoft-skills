using System.Text.Json;
using RabbitMQ.Client;

namespace Orders.Api.Messaging;

// Naive variant: one shared channel, no publisher confirms, classic queue, publish inline after SaveChangesAsync.
public sealed class RabbitPublisher(IConfiguration configuration) : IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task<IChannel> ChannelAsync()
    {
        if (_channel is null)
        {
            var factory = new ConnectionFactory { Uri = new Uri(configuration.GetConnectionString("RabbitMq")!) };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            await _channel.ExchangeDeclareAsync("orders.events", ExchangeType.Topic, durable: true);
            await _channel.QueueDeclareAsync("warehouse.order-placed", durable: true, exclusive: false, autoDelete: false);
            await _channel.QueueBindAsync("warehouse.order-placed", "orders.events", "order.placed.v1");
        }

        return _channel;
    }

    public async Task PublishAsync(object message)
    {
        var channel = await ChannelAsync();
        await channel.BasicPublishAsync("orders.events", "order.placed.v1", JsonSerializer.SerializeToUtf8Bytes(message));
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
    }
}

public sealed class TopologyStartup(RabbitPublisher publisher) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) => await publisher.ChannelAsync();

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
