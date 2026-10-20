using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Orders.Api.Orders;

namespace Orders.Api.Mail;

public sealed class SmtpSettings
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = string.Empty;
}

public sealed class MailSender(IOptions<SmtpSettings> settings, ILogger<MailSender> logger)
{
    public async Task SendOrderConfirmationAsync(Order order, string customerName, string email, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(settings.Value.From));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = "Order received";
            var lines = string.Concat(order.Lines.Select(line => $"<li>{line.Sku} x {line.Quantity}</li>"));
            message.Body = new BodyBuilder
            {
                TextBody = $"Dear {customerName}, we received your order {order.Id}.",
                HtmlBody = $"<p>Dear {customerName},</p><p>We received your order {order.Id}.</p><ul>{lines}</ul>",
            }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Value.Host, settings.Value.Port, SecureSocketOptions.Auto, ct);
            await client.AuthenticateAsync(settings.Value.UserName, settings.Value.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not send the order confirmation for order {OrderId}.", order.Id);
        }
    }
}
