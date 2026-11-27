namespace Kiosk.Api.Mail;

public interface IEmailSender
{
    /// <summary>Adds the mail to the outbox. The worker sends it. The caller saves the unit of work.</summary>
    Task QueueAsync(string recipient, string subject, string body, CancellationToken ct);
}
