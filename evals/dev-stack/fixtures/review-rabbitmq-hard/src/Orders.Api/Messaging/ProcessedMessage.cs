namespace Orders.Api.Messaging;

// A message that a consumer has handled. A redelivery of the same message is skipped.
public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
        MessageId = string.Empty;
        Queue = string.Empty;
    }

    public ProcessedMessage(string messageId, string queue, DateTimeOffset processedAt)
    {
        MessageId = messageId;
        Queue = queue;
        ProcessedAt = processedAt;
    }

    public string MessageId { get; private set; }

    public string Queue { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }
}
