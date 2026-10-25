using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Orders.Api.Messaging;

// Sends the outbox rows to orders.events with publisher confirms. A row is marked sent only after the confirm.
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
                or PublishException or IOException or TimeoutException or OperationCanceledException or DbException)
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
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var batch = await db.OutboxMessages
            .Where(message => message.SentAt == null)
            .OrderBy(message => message.Id)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (batch.Count == 0)
        {
            return 0;
        }

        var channel = await GetChannelAsync(ct);
        foreach (var message in batch)
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.MessageId.ToString(),
                CorrelationId = message.CorrelationId ?? message.MessageId.ToString(),
                ContentType = "application/json",
                Type = message.MessageType,
                Timestamp = new AmqpTimestamp(message.CreatedAt.ToUnixTimeSeconds()),
            };

            // With confirmation tracking on, this await ends when the broker confirms the message.
            await channel.BasicPublishAsync(
                RabbitMqTopology.OrdersExchange,
                message.RoutingKey,
                mandatory: false,
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
            Uri = RabbitMqTopology.BrokerUri(configuration),
            ClientProvidedName = "orders-api.outbox",
        };
        _connection = await factory.CreateConnectionAsync(ct);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        await RabbitMqTopology.DeclareOrdersExchangeAsync(_channel, ct);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "The outbox publisher could not send the outbox rows. It tries again.")]
    private static partial void LogPublishFailed(ILogger logger, Exception exception);
}
