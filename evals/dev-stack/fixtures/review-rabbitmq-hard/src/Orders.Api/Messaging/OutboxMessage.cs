namespace Orders.Api.Messaging;

// An event that waits for the OutboxPublisher. It is written in the same SaveChangesAsync as the business change.
public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        MessageType = string.Empty;
        RoutingKey = string.Empty;
        Payload = string.Empty;
    }

    public OutboxMessage(string messageType, string routingKey, string payload, string correlationId, DateTimeOffset createdAt)
    {
        MessageType = messageType;
        RoutingKey = routingKey;
        Payload = payload;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }

    public Guid MessageId { get; private set; } = Guid.CreateVersion7();

    public string MessageType { get; private set; }

    public string RoutingKey { get; private set; }

    public string Payload { get; private set; }

    public string? CorrelationId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public void MarkSent(DateTimeOffset now) => SentAt = now;
}
