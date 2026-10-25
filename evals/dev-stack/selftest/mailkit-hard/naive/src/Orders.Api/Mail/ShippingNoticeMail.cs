using System.Globalization;
using System.Text;
using MimeKit;
using Orders.Api.Customers;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Mail;

public static class ShippingNoticeMail
{
    public const string Subject = "Your order is on its way";

    public static MimeMessage Create(Customer customer, Order order, Shipment shipment, Carrier carrier)
    {
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress(customer.Name, customer.Email));
        foreach (var address in customer.NotificationAddresses)
        {
            message.To.Add(MailboxAddress.Parse(address));
        }

        message.Subject = Subject;
        var trackingUrl = CarrierCatalog.TrackingUrl(carrier, shipment.TrackingNumber!);
        var lines = new StringBuilder();
        foreach (var line in order.Lines)
        {
            lines.Append(CultureInfo.InvariantCulture, $"<tr><td>{line.Sku}</td><td>{line.Quantity}</td></tr>");
        }

        var text = string.Create(CultureInfo.InvariantCulture, $"""
            Dear {customer.Name},

            Your order {order.Id} is on its way with {carrier.DisplayName}.
            Track it here: {trackingUrl}
            Delivery instructions: {order.DeliveryInstructions}
            """);
        var html = string.Create(CultureInfo.InvariantCulture, $"""
            <html><body style="font-family:Arial,sans-serif">
            <p>Dear {customer.Name},</p>
            <p>Your order <strong>{order.Id}</strong> is on its way with {carrier.DisplayName}.</p>
            <p><a href="{trackingUrl}">Track your parcel ({shipment.TrackingNumber})</a></p>
            <table><tr><th>SKU</th><th>Quantity</th></tr>{lines}</table>
            <p>Delivery instructions: {order.DeliveryInstructions}</p>
            </body></html>
            """);
        message.Body = new BodyBuilder { TextBody = text, HtmlBody = html }.ToMessageBody();
        return message;
    }
}
