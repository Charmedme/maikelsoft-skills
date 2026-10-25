namespace Orders.Api.Returns;

public sealed class ReturnRequest
{
    private ReturnRequest()
    {
    }

    public ReturnRequest(Guid orderId, ReturnReason reason, string? comment, DateTimeOffset requestedAt)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("The order id must not be empty.", nameof(orderId));
        }

        OrderId = orderId;
        Reason = reason;
        Comment = comment;
        RequestedAt = requestedAt;
        Status = ReturnStatus.Requested;
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public ReturnReason Reason { get; private set; }

    public string? Comment { get; private set; }

    public ReturnStatus Status { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? DecidedAt { get; private set; }

    public void Approve(DateTimeOffset now) => Decide(ReturnStatus.Approved, now);

    public void Reject(DateTimeOffset now) => Decide(ReturnStatus.Rejected, now);

    private void Decide(ReturnStatus status, DateTimeOffset now)
    {
        if (Status != ReturnStatus.Requested)
        {
            throw new InvalidOperationException("The return is already decided.");
        }

        Status = status;
        DecidedAt = now;
    }
}
