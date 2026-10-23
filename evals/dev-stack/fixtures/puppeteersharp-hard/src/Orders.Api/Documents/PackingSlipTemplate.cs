using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Orders.Api.Customers;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Documents;

public static class PackingSlipTemplate
{
    public static string Render(Shipment shipment, Order order, Customer customer, DocumentOptions options)
    {
        var e = HtmlEncoder.Default;
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        html.Append(DocumentStyles.FontLink);
        html.Append("<style>").Append(DocumentStyles.Css).Append("</style></head><body>");
        html.Append(CultureInfo.InvariantCulture, $"<div class=\"header\"><img src=\"{e.Encode(options.LogoUrl.AbsoluteUri)}\" alt=\"\"><span>Packing slip</span></div>");
        html.Append(CultureInfo.InvariantCulture, $"<p><strong>{e.Encode(customer.DisplayName)}</strong><br>Order {order.Id}<br>Shipment {shipment.Id}</p>");
        html.Append("<table><tr><th>SKU</th><th>Quantity</th></tr>");
        foreach (var line in order.Lines)
        {
            html.Append(CultureInfo.InvariantCulture, $"<tr><td>{e.Encode(line.Sku)}</td><td>{line.Quantity}</td></tr>");
        }

        html.Append("</table>");
        if (!string.IsNullOrWhiteSpace(order.DeliveryInstructions))
        {
            html.Append(CultureInfo.InvariantCulture, $"<div class=\"note\">{e.Encode(order.DeliveryInstructions)}</div>");
        }

        html.Append(CultureInfo.InvariantCulture, $"<div class=\"footer\">{e.Encode(options.CompanyName)}, {e.Encode(options.CompanyAddress)}</div>");
        html.Append("</body></html>");
        return html.ToString();
    }
}
