using System.ComponentModel.DataAnnotations;
using MailKit.Security;

namespace Orders.Api.Mail;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    [Required]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    // Auto picks the best mode that the relay offers.
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.Auto;

    public string UserName { get; set; } = string.Empty;

    // Set Smtp__Password in the environment.
    public string Password { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string From { get; set; } = string.Empty;
}
