using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Data;
using Orders.Api.Messaging;
using Orders.Api.Messaging.Contracts;
using RabbitMQ.Client;

namespace Orders.Api.Payments;

// Stores the payment results of the payment gateway. A result is identified by its paymentId: the gateway can
// send the same result again with a new message_id. A result that cannot be stored goes to orders.dlq for finance.
public sealed class PaymentResultConsumer(RabbitMqConnection rabbit, IServiceScopeFactory scopes, ILogger<PaymentResultConsumer> logger)
    : RabbitMqConsumer<PaymentResultV1>(rabbit, scopes, logger)
{
    protected override string QueueName => RabbitMqTopology.PaymentResultsQueue;

    protected override string Exchange => RabbitMqTopology.PaymentsExchange;

    protected override IReadOnlyList<string> RoutingKeys => [PaymentResultV1.CapturedWireName, PaymentResultV1.FailedWireName];

    protected override async Task HandleAsync(PaymentResultV1 message, string routingKey, IReadOnlyBasicProperties properties, IServiceProvider services, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.PaymentId) || message.PaymentId.Length > 64 || message.OrderId == Guid.Empty || message.Currency is not { Length: 3 })
        {
            throw new InvalidOperationException("The payment result has no valid paymentId, orderId or currency.");
        }

        var db = services.GetRequiredService<OrdersDbContext>();
        if (await db.Payments.AnyAsync(p => p.ProviderReference == message.PaymentId, ct))
        {
            return; // The same result again.
        }

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, ct)
            ?? throw new InvalidOperationException($"The order {message.OrderId} of payment {message.PaymentId} does not exist.");

        switch (routingKey)
        {
            case PaymentResultV1.CapturedWireName:
                if (!order.CanBePaid)
                {
                    throw new InvalidOperationException($"The order {order.Id} has the status {order.Status} and cannot be paid.");
                }

                var paidAt = message.CapturedAt ?? throw new InvalidOperationException("capturedAt is missing.");
                db.Payments.Add(Payment.Captured(order.Id, message.Amount, message.Currency, message.PaymentId, paidAt));
                order.MarkPaid(paidAt);
                db.Enqueue(new OrderPaidV1(order.Id, message.Amount, message.Currency, paidAt), OrderPaidV1.WireName, order.Id.ToString(), paidAt);
                break;
            case PaymentResultV1.FailedWireName:
                var reason = message.Reason is { Length: > 0 and <= 64 } r ? r : "unknown";
                db.Payments.Add(Payment.Failed(order.Id, message.Amount, message.Currency, message.PaymentId, reason, message.FailedAt ?? DateTimeOffset.UtcNow));
                break;
            default:
                throw new InvalidOperationException($"Unknown routing key {routingKey}.");
        }

        try
        {
            await db.SaveChangesAsync(ct); // the payment, the order status and the outbox row in one transaction
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another delivery of the same result was stored first.
        }
    }
}
