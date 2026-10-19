using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace Orders.Api.Mail;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    // StartTls on 587, SslOnConnect on 465. None only for a local test relay.
    [AllowedValues(SecureSocketOptions.StartTls, SecureSocketOptions.SslOnConnect, SecureSocketOptions.None)]
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    // From user secrets or the environment (Smtp__Password), never from appsettings.json.
    public string? Password { get; set; }

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    public string FromName { get; set; } = "Orders";

    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;
}
