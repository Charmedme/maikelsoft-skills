using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Orders.Api.Messaging;

// The base class of the consumers of this service. A derived class names its queue, its bindings,
// and handles one message type. The base declares the topology, acks after the handler, and classifies a failure:
// a permanent failure goes to the dead-letter exchange at once, a transient failure is returned with
// BasicRejectAsync, which counts toward the delivery-limit of the queue (RabbitMQ 4.3 does not count a nack).
public abstract partial class RabbitMqConsumer<TMessage>(RabbitMqConnection rabbit, IServiceScopeFactory scopes, ILogger logger)
    : BackgroundService
    where TMessage : class
{
    private const ushort Prefetch = 20;
    private static readonly TimeSpan s_reconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    protected abstract string QueueName { get; }

    protected abstract string Exchange { get; }

    protected abstract IReadOnlyList<string> RoutingKeys { get; }

    // The arguments of an existing queue cannot change. A new queue can override them.
    protected virtual IDictionary<string, object?> QueueArguments => RabbitMqTopology.WorkQueueArguments();

    protected abstract Task HandleAsync(TMessage message, string routingKey, IReadOnlyBasicProperties properties, IServiceProvider services, CancellationToken ct);

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
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: QueueArguments, cancellationToken: ct);
        foreach (var routingKey in RoutingKeys)
        {
            await channel.QueueBindAsync(QueueName, Exchange, routingKey, cancellationToken: ct);
        }

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: Prefetch, global: false, ct);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                // Read the body here: the client reuses its memory after this handler returns.
                var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span, s_json)
                    ?? throw new PermanentMessageException("The message body is empty.");
                await using var scope = scopes.CreateAsyncScope();
                await HandleAsync(message, ea.RoutingKey, ea.BasicProperties, scope.ServiceProvider, ct);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, ct);
            }
            catch (Exception ex) when (ex is JsonException or PermanentMessageException or ArgumentException)
            {
                LogPermanentFailure(logger, ex, QueueName, ea.BasicProperties.MessageId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, ct); // to the dead-letter exchange
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogTransientFailure(logger, ex, QueueName, ea.BasicProperties.MessageId);
                await channel.BasicRejectAsync(ea.DeliveryTag, requeue: true, ct); // counts toward the delivery-limit
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

    [LoggerMessage(Level = LogLevel.Error, Message = "Message {MessageId} from {Queue} cannot be processed. It goes to the dead-letter queue.")]
    private static partial void LogPermanentFailure(ILogger logger, Exception exception, string queue, string? messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Handling message {MessageId} from {Queue} failed. It comes again.")]
    private static partial void LogTransientFailure(ILogger logger, Exception exception, string queue, string? messageId);
}
