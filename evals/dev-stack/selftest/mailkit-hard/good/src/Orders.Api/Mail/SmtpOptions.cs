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

    // StartTls on 587, SslOnConnect on 465. None only for a local test relay (checked in Program.cs).
    [AllowedValues(SecureSocketOptions.StartTls, SecureSocketOptions.SslOnConnect, SecureSocketOptions.None)]
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; } // user secrets or Smtp__Password

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = "Example Shop";

    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;
}
