using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Orders.Api.Customers;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Mail;

public static class ShippingNoticeMail
{
    public const string Subject = "Your order is on its way";

    // One notice for the customer and the notification addresses: the goods-in team sees who else got it.
    public static OutboxMail Create(Customer customer, Order order, Shipment shipment, Carrier carrier, DateTimeOffset now)
    {
        var trackingUrl = CarrierCatalog.TrackingUrl(carrier, shipment.TrackingNumber!);
        var text = RenderText(customer, order, carrier, trackingUrl);
        var html = RenderHtml(customer, order, shipment, carrier, trackingUrl);
        var recipients = customer.NotificationAddresses
            .Prepend(customer.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return new OutboxMail(string.Join(", ", recipients), customer.DisplayName, Subject, text, html, now);
    }

    private static string RenderText(Customer customer, Order order, Carrier carrier, Uri trackingUrl)
    {
        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"Dear {customer.Name},\n\nYour order {order.Id} is on its way with {carrier.DisplayName}.\n");
        text.Append(CultureInfo.InvariantCulture, $"Track it here: {trackingUrl}\n\n");
        foreach (var line in order.Lines)
        {
            text.Append(CultureInfo.InvariantCulture, $"{line.Sku}  {line.Quantity} x {line.UnitPrice:0.00}\n");
        }

        if (!string.IsNullOrWhiteSpace(order.DeliveryInstructions))
        {
            text.Append(CultureInfo.InvariantCulture, $"\nDelivery instructions: {order.DeliveryInstructions}\n");
        }

        return text.ToString();
    }

    private static string RenderHtml(Customer customer, Order order, Shipment shipment, Carrier carrier, Uri trackingUrl)
    {
        var e = HtmlEncoder.Default;
        var html = new StringBuilder();
        html.Append("<html><body style=\"font-family:Arial,sans-serif\">");
        html.Append(CultureInfo.InvariantCulture, $"<p>Dear {e.Encode(customer.Name)},</p>");
        html.Append(CultureInfo.InvariantCulture, $"<p>Your order <strong>{order.Id}</strong> is on its way with {e.Encode(carrier.DisplayName)}.</p>");
        html.Append(CultureInfo.InvariantCulture, $"<p><a href=\"{e.Encode(trackingUrl.AbsoluteUri)}\">Track your parcel ({e.Encode(shipment.TrackingNumber!)})</a></p>");
        html.Append("<table><tr><th>SKU</th><th>Quantity</th></tr>");
        foreach (var line in order.Lines)
        {
            html.Append(CultureInfo.InvariantCulture, $"<tr><td>{e.Encode(line.Sku)}</td><td>{line.Quantity}</td></tr>");
        }

        html.Append("</table>");
        if (!string.IsNullOrWhiteSpace(order.DeliveryInstructions))
        {
            html.Append(CultureInfo.InvariantCulture, $"<p style=\"white-space:pre-wrap\">Delivery instructions: {e.Encode(order.DeliveryInstructions)}</p>");
        }

        html.Append("</body></html>");
        return html.ToString();
    }
}
