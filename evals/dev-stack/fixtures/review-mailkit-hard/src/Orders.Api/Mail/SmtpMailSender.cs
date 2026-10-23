using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Orders.Api.Mail;

public sealed class SmtpMailSender(IOptions<SmtpOptions> options) : IMailSender
{
    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        var smtp = options.Value;
        if (message.From.Count == 0)
        {
            message.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
        }

        using var client = new SmtpClient { Timeout = smtp.TimeoutMs };
        await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);
        if (!string.IsNullOrEmpty(smtp.UserName))
        {
            await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
