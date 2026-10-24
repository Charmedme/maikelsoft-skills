using MimeKit;

namespace Orders.Api.Mail;

public static class MailAddresses
{
    // One plain address: no CR or LF (CVE-2026-30227), no list, no display name.
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 254
        && value.IndexOfAny(['\r', '\n']) < 0
        && MailboxAddress.TryParse(value, out var mailbox)
        && string.Equals(mailbox.Address, value.Trim(), StringComparison.OrdinalIgnoreCase);
}
