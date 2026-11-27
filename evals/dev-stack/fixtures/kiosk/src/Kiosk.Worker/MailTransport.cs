namespace Kiosk.Worker;

public sealed record OutgoingMail(long Id, string To, string Subject, string Body);

public interface IMailTransport
{
    Task SendAsync(OutgoingMail mail, CancellationToken ct);
}

/// <summary>Writes the mail to the log. The SMTP transport is configured per environment.</summary>
public sealed class LoggingMailTransport(ILogger<LoggingMailTransport> logger) : IMailTransport
{
    public Task SendAsync(OutgoingMail mail, CancellationToken ct)
    {
        logger.LogInformation("Sent outbox mail {MailId}.", mail.Id);
        return Task.CompletedTask;
    }
}
