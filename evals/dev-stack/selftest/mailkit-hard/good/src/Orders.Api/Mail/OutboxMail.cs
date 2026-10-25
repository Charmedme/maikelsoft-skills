namespace Orders.Api.Mail;

public sealed class OutboxMail
{
    private OutboxMail()
    {
        ToAddress = string.Empty;
        ToName = string.Empty;
        Subject = string.Empty;
        TextBody = string.Empty;
        HtmlBody = string.Empty;
    }

    public OutboxMail(string toAddress, string toName, string subject, string textBody, string htmlBody, DateTimeOffset now)
    {
        ToAddress = toAddress;
        ToName = toName;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
        CreatedAt = now;
        NextAttemptAt = now;
    }

    public Guid Id { get; private set; }

    public string ToAddress { get; private set; }

    public string ToName { get; private set; }

    public string Subject { get; private set; }

    public string TextBody { get; private set; }

    public string HtmlBody { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public string? LastError { get; private set; }

    public void MarkSent(DateTimeOffset now) => SentAt = now;

    public void MarkFailed(DateTimeOffset now, string error)
    {
        FailedAt = now;
        LastError = error;
    }

    public void Retry(DateTimeOffset now, string error)
    {
        Attempts++;
        LastError = error;

        // 5 s, 10 s, 20 s, then every 30 s.
        var delay = TimeSpan.FromSeconds(Math.Min(30, 5 * Math.Pow(2, Math.Min(Attempts - 1, 4))));
        NextAttemptAt = now + delay;
    }
}
