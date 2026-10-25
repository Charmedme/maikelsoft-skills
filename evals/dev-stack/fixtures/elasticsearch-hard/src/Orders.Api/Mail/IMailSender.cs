using MimeKit;

namespace Orders.Api.Mail;

public interface IMailSender
{
    Task SendAsync(MimeMessage message, CancellationToken ct);
}
