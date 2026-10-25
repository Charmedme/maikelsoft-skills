using System.Globalization;
using MimeKit;
using Orders.Api.Customers;
using Orders.Api.Returns;

namespace Orders.Api.Mail;

public static class ReturnReceivedMail
{
    public const string Subject = "We received your return request";

    public static MimeMessage Create(Customer customer, ReturnRequest request)
    {
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress(customer.Name, customer.Email));
        message.Subject = Subject;

        var text = string.Create(CultureInfo.InvariantCulture, $"""
            Dear {customer.Name},

            We received your return request for order {request.OrderId}.
            Reason: {request.Reason}
            Your comment: {request.Comment}

            We will send you a return label within two working days.
            """);
        var html = string.Create(CultureInfo.InvariantCulture, $"""
            <html><body style="font-family:Arial,sans-serif">
            <p>Dear {customer.Name},</p>
            <p>We received your return request for order <strong>{request.OrderId}</strong>.</p>
            <p>Reason: {request.Reason}<br>Your comment: {request.Comment}</p>
            <p>We will send you a return label within two working days.</p>
            </body></html>
            """);
        message.Body = new BodyBuilder { TextBody = text, HtmlBody = html }.ToMessageBody();
        return message;
    }
}
