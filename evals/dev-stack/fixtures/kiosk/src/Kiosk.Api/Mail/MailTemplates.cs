using System.Text.Encodings.Web;

namespace Kiosk.Api.Mail;

public static class MailTemplates
{
    public static string PasswordReset(string name, string link) =>
        $"""
        <p>Hello {HtmlEncoder.Default.Encode(name)},</p>
        <p>Use this link to choose a new password. The link works for one hour.</p>
        <p><a href="{HtmlEncoder.Default.Encode(link)}">{HtmlEncoder.Default.Encode(link)}</a></p>
        <p>If you did not ask for a new password, you can ignore this mail.</p>
        """;

    public static string OrderConfirmation(string orderNumber, decimal total) =>
        $"<p>Thank you for your order {HtmlEncoder.Default.Encode(orderNumber)}. Total: {total:0.00} EUR.</p>";
}
