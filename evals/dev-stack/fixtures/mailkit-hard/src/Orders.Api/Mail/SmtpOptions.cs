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

    // Use TLS when the relay offers it. The internal relay of the test environment has no certificate.
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTlsWhenAvailable;

    public string? UserName { get; set; }

    public string? Password { get; set; } // user secrets or Smtp__Password

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;

    [Required]
    public string FromName { get; set; } = "Example Shop";

    [Range(1000, 300000)]
    public int TimeoutMs { get; set; } = 30000;
}
