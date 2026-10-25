namespace Orders.Api.Messaging.Contracts;

// Published on orders.events when an order is cancelled. The warehouse drops the picking slot.
public sealed record OrderCancelledV1(Guid OrderId, DateTimeOffset CancelledAt)
{
    public const string WireName = "order.cancelled.v1";
}
