namespace Orders.Api.Orders;

public sealed class OrderEvent
{
    private OrderEvent()
    {
        Type = string.Empty;
    }

    public OrderEvent(Guid orderId, string type, DateTimeOffset occurredAt)
    {
        OrderId = orderId;
        Type = type;
        OccurredAt = occurredAt;
    }

    public long Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Type { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
}
