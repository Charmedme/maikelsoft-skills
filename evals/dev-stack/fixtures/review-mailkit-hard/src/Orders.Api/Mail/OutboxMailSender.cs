using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Orders.Api.Data;

namespace Orders.Api.Mail;

public sealed class OutboxMailSender(
    IServiceScopeFactory scopes,
    IOptions<SmtpOptions> options,
    TimeProvider time,
    ILogger<OutboxMailSender> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_giveUpAfter = TimeSpan.FromDays(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var sent = 0;
            try
            {
                sent = await SendDueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "The mail outbox batch failed.");
            }

            if (sent < BatchSize)
            {
                await Task.Delay(s_pollInterval, time, stoppingToken);
            }
        }
    }

    private async Task<int> SendDueAsync(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var now = time.GetUtcNow();
        var due = await db.OutboxMails
            .Where(mail => mail.SentAt == null && mail.FailedAt == null && mail.NextAttemptAt <= now)
            .OrderBy(mail => mail.NextAttemptAt)
            .ThenBy(mail => mail.Id)
            .Take(BatchSize)
            .ToListAsync(ct);
        if (due.Count == 0)
        {
            return 0;
        }

        var smtp = options.Value;
        var sent = 0;
        using var client = new SmtpClient { Timeout = smtp.TimeoutMs };
        try
        {
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);
            if (!string.IsNullOrEmpty(smtp.UserName))
            {
                await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, ct);
            }

            foreach (var mail in due)
            {
                using var message = ToMessage(mail, smtp);
                try
                {
                    await client.SendAsync(message, ct);
                    mail.MarkSent(time.GetUtcNow());
                    sent++;
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Sent mail {MailId} to {Recipients}: {Body}", mail.Id, mail.ToAddress, mail.TextBody);
                    }
                }
                catch (SmtpCommandException ex)
                {
                    // The relay answered with an error code: try this mail again later.
                    Postpone(mail, $"{(int)ex.StatusCode} {ex.ErrorCode}");
                }
            }

            await client.DisconnectAsync(true, ct);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (ex is SocketException or IOException or SmtpProtocolException or ServiceNotConnectedException or SslHandshakeException)
        {
            logger.LogWarning("The SMTP relay is not reachable: {Error}.", ex.GetType().Name);
            PostponeUnsent(due, ex.GetType().Name);
        }
        catch (Exception ex) when (ex is AuthenticationException or NotSupportedException)
        {
            logger.LogError("The SMTP relay refused the connection settings: {Error}.", ex.GetType().Name);
            PostponeUnsent(due, ex.GetType().Name);
        }

        return sent;
    }

    private void PostponeUnsent(List<OutboxMail> due, string error)
    {
        foreach (var mail in due.Where(mail => mail.SentAt == null && mail.FailedAt == null))
        {
            Postpone(mail, error);
        }
    }

    private void Postpone(OutboxMail mail, string error)
    {
        var now = time.GetUtcNow();
        if (now - mail.CreatedAt > s_giveUpAfter)
        {
            mail.MarkFailed(now, error);
            logger.LogError("Mail {MailId} was not sent within {Hours} hours.", mail.Id, s_giveUpAfter.TotalHours);
            return;
        }

        mail.Retry(now, error);
    }

    private static MimeMessage ToMessage(OutboxMail mail, SmtpOptions smtp)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
        message.To.AddRange(InternetAddressList.Parse(mail.ToAddress));
        message.Subject = mail.Subject;
        message.MessageId = $"{mail.Id:N}@{smtp.From.Split('@')[1]}";
        message.Body = new BodyBuilder { TextBody = mail.TextBody, HtmlBody = mail.HtmlBody }.ToMessageBody();
        return message;
    }
}
