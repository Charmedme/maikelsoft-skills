namespace Orders.Api.Returns;

public sealed class OrderReturn
{
    private OrderReturn()
    {
        Sku = string.Empty;
    }

    public OrderReturn(Guid orderId, string sku, int quantity, ReturnReason reason, string? comment, DateTimeOffset requestedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        OrderId = orderId;
        Sku = sku;
        Quantity = quantity;
        Reason = reason;
        Comment = comment;
        RequestedAt = requestedAt;
        Status = ReturnStatus.Requested;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public ReturnReason Reason { get; private set; }

    public string? Comment { get; private set; }

    public ReturnStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }
}
