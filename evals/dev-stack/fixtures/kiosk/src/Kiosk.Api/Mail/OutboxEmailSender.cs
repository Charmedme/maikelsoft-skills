using Kiosk.Api.Data;

namespace Kiosk.Api.Mail;

public sealed class OutboxEmailSender(KioskDbContext db, TimeProvider time) : IEmailSender
{
    public Task QueueAsync(string recipient, string subject, string body, CancellationToken ct)
    {
        db.OutboxEmails.Add(new OutboxEmail { To = recipient, Subject = subject, Body = body, CreatedAt = time.GetUtcNow() });
        return Task.CompletedTask;
    }
}
