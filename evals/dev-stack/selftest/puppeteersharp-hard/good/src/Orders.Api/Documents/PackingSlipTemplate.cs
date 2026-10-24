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
        html.Append(DocumentStyles.ContentSecurityPolicy);
        html.Append("<style>").Append(DocumentStyles.Css).Append("</style></head><body>");
        html.Append(CultureInfo.InvariantCulture, $"<div class=\"header\"><img src=\"{DocumentAssets.LogoDataUri}\" alt=\"\">");
        if (customer.BrandingLogo is not null && customer.BrandingLogoContentType is not null)
        {
            var logo = new LogoImage(customer.BrandingLogo, customer.BrandingLogoContentType);
            html.Append(CultureInfo.InvariantCulture, $"<img src=\"{logo.DataUri}\" alt=\"\">");
        }

        html.Append("<span>Packing slip</span></div>");
        html.Append(CultureInfo.InvariantCulture, $"<p><strong>{e.Encode(customer.DisplayName)}</strong><br>Order {order.Id}<br>Shipment {shipment.Id}");
        if (!string.IsNullOrWhiteSpace(order.CustomerReference))
        {
            html.Append(CultureInfo.InvariantCulture, $"<br>Your reference {e.Encode(order.CustomerReference)}");
        }

        html.Append("</p>");
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

        if (customer.BrandingFooter is not null)
        {
            html.Append(CultureInfo.InvariantCulture, $"<div class=\"footer\">{e.Encode(customer.BrandingFooter)}</div>");
        }

        html.Append(CultureInfo.InvariantCulture, $"<div class=\"footer\">{e.Encode(options.CompanyName)}, {e.Encode(options.CompanyAddress)}</div>");
        html.Append("</body></html>");
        return html.ToString();
    }
}
