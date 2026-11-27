namespace Kiosk.Api.Mail;

public sealed class OutboxEmail
{
    public long Id { get; set; }

    public required string To { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public int Attempts { get; set; }
}
