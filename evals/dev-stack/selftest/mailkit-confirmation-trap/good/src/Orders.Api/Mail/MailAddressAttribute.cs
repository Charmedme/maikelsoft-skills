using System.ComponentModel.DataAnnotations;
using MimeKit;

namespace Orders.Api.Mail;

// One mailbox address without CR or LF. A CR or LF in an address is header injection (CVE-2026-30227).
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MailAddressAttribute : ValidationAttribute
{
    public MailAddressAttribute()
        : base("The field {0} must be one valid email address.")
    {
    }

    public override bool IsValid(object? value) =>
        value is not string text
        || (text.AsSpan().IndexOfAny('\r', '\n') < 0
            && MailboxAddress.TryParse(text, out var address)
            && string.IsNullOrEmpty(address.Name)
            && address.Address.Contains('@', StringComparison.Ordinal));
}
