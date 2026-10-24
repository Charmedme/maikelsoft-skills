namespace Orders.Api.Messaging.Contracts;

// Consumed from payments.events (docs/payment-gateway-events.md). One record for both events:
// payment.captured.v1 has CapturedAt, payment.failed.v1 has Reason and FailedAt.
public sealed record PaymentResultV1(
    string PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTimeOffset? CapturedAt,
    string? Reason,
    DateTimeOffset? FailedAt)
{
    public const string CapturedWireName = "payment.captured.v1";
    public const string FailedWireName = "payment.failed.v1";
}
