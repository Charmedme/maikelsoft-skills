namespace Orders.Api.Messaging.Contracts;

// Published on orders.events when the payment of an order is captured. The warehouse can pick the order.
public sealed record OrderPaidV1(Guid OrderId, decimal Amount, string Currency, DateTimeOffset PaidAt)
{
    public const string WireName = "order.paid.v1";
}
