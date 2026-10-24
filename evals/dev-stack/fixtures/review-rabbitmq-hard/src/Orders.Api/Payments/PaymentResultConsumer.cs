using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Messaging;
using Orders.Api.Messaging.Contracts;
using Orders.Api.Orders;
using RabbitMQ.Client;

namespace Orders.Api.Payments;

// Stores the payment results of the payment gateway (docs/payment-gateway-events.md).
public sealed partial class PaymentResultConsumer(
    RabbitMqConnection rabbit,
    IServiceScopeFactory scopes,
    OrderEventsPublisher events,
    TimeProvider time,
    ILogger<PaymentResultConsumer> logger)
    : RabbitMqConsumer<PaymentResultV1>(rabbit, scopes, logger)
{
    protected override string QueueName => RabbitMqTopology.PaymentResultsQueue;

    protected override string Exchange => RabbitMqTopology.PaymentsExchange;

    protected override IReadOnlyList<string> RoutingKeys => [PaymentResultV1.CapturedWireName, PaymentResultV1.FailedWireName];

    protected override async Task HandleAsync(PaymentResultV1 message, string routingKey, IReadOnlyBasicProperties properties, IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<OrdersDbContext>();
        var messageId = properties.MessageId ?? throw new InvalidOperationException("The payment result has no message_id.");
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId, ct))
        {
            return; // A redelivery of a result that we already stored.
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, ct);
        if (order is null)
        {
            LogUnknownOrder(logger, message.PaymentId, message.OrderId);
            return;
        }

        var now = time.GetUtcNow();
        switch (routingKey)
        {
            case PaymentResultV1.CapturedWireName:
                var paidAt = message.CapturedAt ?? now;
                db.Payments.Add(Payment.Captured(order.Id, message.Amount, message.Currency, message.PaymentId, paidAt));
                order.MarkPaid(paidAt);
                break;
            case PaymentResultV1.FailedWireName:
                db.Payments.Add(Payment.Failed(order.Id, message.Amount, message.Currency, message.PaymentId, message.Reason ?? "unknown", message.FailedAt ?? now));
                order.Cancel(now, TimeSpan.MaxValue);
                break;
            default:
                throw new InvalidOperationException($"Unknown payment event {routingKey}.");
        }

        db.ProcessedMessages.Add(new ProcessedMessage(messageId, QueueName, now));
        await db.SaveChangesAsync(ct);

        if (order.Status == OrderStatus.Paid)
        {
            await events.PublishAsync(new OrderPaidV1(order.Id, message.Amount, message.Currency, order.PaidAt!.Value), OrderPaidV1.WireName, order.Id.ToString(), now, ct);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Payment {PaymentId} is for the unknown order {OrderId}. It is skipped.")]
    private static partial void LogUnknownOrder(ILogger logger, string paymentId, Guid orderId);
}
