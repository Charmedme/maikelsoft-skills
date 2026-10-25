namespace Orders.Api.Messaging;

// A message that can never succeed. The consumer sends it to the dead-letter exchange without a retry.
public sealed class PermanentMessageException : Exception
{
    public PermanentMessageException()
    {
    }

    public PermanentMessageException(string message)
        : base(message)
    {
    }

    public PermanentMessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
