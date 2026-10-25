namespace Orders.Api.Returns;

public sealed class ReturnRequest
{
    private ReturnRequest()
    {
        Reason = string.Empty;
    }

    public ReturnRequest(Guid orderId, string reason, DateTimeOffset requestedAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("The order id must not be empty.", nameof(orderId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        OrderId = orderId;
        Reason = reason;
        RequestedAt = requestedAt;
        Status = ReturnStatus.Requested;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public string Reason { get; private set; }

    public ReturnStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? ReceivedAt { get; private set; }

    public bool MarkReceived(DateTimeOffset now)
    {
        if (Status != ReturnStatus.Requested)
        {
            return false;
        }

        Status = ReturnStatus.Received;
        ReceivedAt = now;
        return true;
    }
}
