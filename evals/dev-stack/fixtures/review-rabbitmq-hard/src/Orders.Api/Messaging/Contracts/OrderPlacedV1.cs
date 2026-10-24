namespace Orders.Api.Messaging.Contracts;

// Published on orders.events when an order is placed. The warehouse reserves the picking slot.
public sealed record OrderPlacedV1(Guid OrderId, Guid CustomerId, decimal Total, int LineCount, DateTimeOffset PlacedAt)
{
    public const string WireName = "order.placed.v1";
}
