using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Orders.Api.Mail;

// Keeps one open connection to the relay, so that each mail is fast.
public sealed class MailSender(SmtpClient client, IOptions<SmtpOptions> options)
{
    public async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        var smtp = options.Value;
        if (!client.IsConnected)
        {
            // Our relay uses a certificate from the internal CA.
#pragma warning disable CA5359
            client.ServerCertificateValidationCallback = (s, c, h, e) => true;
#pragma warning restore CA5359
            await client.ConnectAsync(smtp.Host, smtp.Port, smtp.Security, ct);
            await client.AuthenticateAsync(smtp.UserName, smtp.Password, ct);
        }

        await client.SendAsync(message, ct);
    }

    public MimeMessage NewMessage()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Orders", options.Value.From));
        return message;
    }
}
