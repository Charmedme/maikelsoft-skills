using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Orders.Api.Messaging;

// The base class of the consumers of this service. A derived class names its queue, its binding,
// and handles one message type. The base declares the topology, acks after the handler, and retries a failure.
public abstract partial class RabbitMqConsumer<TMessage>(RabbitMqConnection rabbit, IServiceScopeFactory scopes, ILogger logger)
    : BackgroundService
    where TMessage : class
{
    private static readonly TimeSpan s_reconnectDelay = TimeSpan.FromSeconds(5);

    protected abstract string QueueName { get; }

    protected abstract string Exchange { get; }

    protected abstract string RoutingKey { get; }

    protected abstract Task HandleAsync(TMessage message, IReadOnlyBasicProperties properties, IServiceProvider services, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is BrokerUnreachableException or AlreadyClosedException or OperationInterruptedException or IOException)
            {
                LogConnectFailed(logger, ex, QueueName);
                await Task.Delay(s_reconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        var connection = await rabbit.GetAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await RabbitMqTopology.DeclareDeadLettersAsync(channel, ct);
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);
        await channel.QueueDeclareAsync(
            QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: RabbitMqTopology.WorkQueueArguments(),
            cancellationToken: ct);
        await channel.QueueBindAsync(QueueName, Exchange, RoutingKey, cancellationToken: ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span)
                ?? throw new JsonException($"The body of message {ea.BasicProperties.MessageId} is empty.");
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await HandleAsync(message, ea.BasicProperties, scope.ServiceProvider, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogHandlerFailed(logger, ex, QueueName, ea.BasicProperties.MessageId);

                // Try again. The delivery-limit of the queue (20) moves a message that fails each time to orders.dlq.
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, ct);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, ct);
        LogConsuming(logger, QueueName);

        // The client recovers the connection and the consumer after a network failure.
        await Task.Delay(Timeout.Infinite, ct);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consuming from {Queue}.")]
    private static partial void LogConsuming(ILogger logger, string queue);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not consume from {Queue}. Trying again.")]
    private static partial void LogConnectFailed(ILogger logger, Exception exception, string queue);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling message {MessageId} from {Queue} failed.")]
    private static partial void LogHandlerFailed(ILogger logger, Exception exception, string queue, string? messageId);
}
