using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Billing.Worker;

public sealed partial class OrderPlacedConsumer(
    IConfiguration configuration,
    InvoiceStore invoices,
    ILogger<OrderPlacedConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);
    private readonly List<ReadOnlyMemory<byte>> _pending = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(configuration.GetConnectionString("RabbitMq")!) };
        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await Topology.DeclareAsync(channel, stoppingToken);
        await channel.BasicQosAsync(0, 50, global: true, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            _pending.Add(ea.Body);
            try
            {
                await FlushAsync();
            }
            catch (Exception ex)
            {
                LogFailed(logger, ex);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(Topology.Queue, autoAck: true, consumer, stoppingToken);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task FlushAsync()
    {
        foreach (var body in _pending)
        {
            var message = JsonSerializer.Deserialize<OrderPlaced>(body.Span, s_json)!;
            invoices.CreateForOrder(message);
        }

        _pending.Clear();
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "OrderPlaced failed, requeued")]
    private static partial void LogFailed(ILogger logger, Exception exception);
}
