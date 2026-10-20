using System.Globalization;
using MimeKit;
using Orders.Api.Customers;

namespace Orders.Api.Mail;

public sealed class InvoiceMail
{
    public MimeEntity CreateBody(Customer customer, DateOnly month, decimal total)
    {
        var period = month.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        var amount = total.ToString("0.00", CultureInfo.InvariantCulture);
        var builder = new BodyBuilder
        {
            TextBody = $"Dear {customer.Name}, your invoice for {period} is {amount} EUR.",
            HtmlBody = $"<p>Dear {customer.Name}</p><p>Your invoice for {period} is <b>{amount} EUR</b>.</p>",
        };
        return builder.ToMessageBody();
    }
}
