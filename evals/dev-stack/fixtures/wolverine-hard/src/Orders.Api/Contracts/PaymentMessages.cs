using Wolverine;
using Wolverine.Attributes;

namespace Orders.Api.Contracts;

// Contracts of the payments service. The payments team owns this file (see docs/payment-events.md).

[MessageIdentity("payments.payment-captured", Version = 1)]
public sealed record PaymentCaptured(Guid PaymentId, Guid OrderId, decimal Amount, string Currency, DateTimeOffset CapturedAt)
    : IForwardsTo<PaymentCapturedV2>
{
    // Version 1 has no shipping speed. Customers who paid before version 2 get standard delivery.
    public PaymentCapturedV2 Transform() => new(PaymentId, OrderId, Amount, Currency, CapturedAt, ShippingSpeed.Standard);
}

[MessageIdentity("payments.payment-captured", Version = 2)]
public sealed record PaymentCapturedV2(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTimeOffset CapturedAt,
    ShippingSpeed ShippingSpeed);

public enum ShippingSpeed
{
    Standard = 0,
    Express = 1,
}
