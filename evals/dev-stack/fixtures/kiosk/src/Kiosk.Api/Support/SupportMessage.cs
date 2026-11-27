namespace Kiosk.Api.Support;

public sealed class SupportMessage
{
    public long Id { get; set; }

    public required string ConversationId { get; set; }

    public string Sender { get; set; } = string.Empty;

    public required string Text { get; set; }

    public DateTimeOffset SentAt { get; set; }
}
