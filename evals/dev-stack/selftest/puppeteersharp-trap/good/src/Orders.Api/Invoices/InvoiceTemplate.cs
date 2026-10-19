using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Orders.Api.Orders;

namespace Orders.Api.Invoices;

public static class InvoiceTemplate
{
    private static readonly CultureInfo s_money = CultureInfo.GetCultureInfo("nl-NL");

    public static string Render(Order order)
    {
        var html = HtmlEncoder.Default;
        var sb = new StringBuilder();
        sb.Append("""
            <!doctype html><html><head><meta charset="utf-8">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'">
            <style>body{font-family:'Liberation Sans',sans-serif} .note{white-space:pre-wrap}</style>
            </head><body>
            """);
        sb.Append(CultureInfo.InvariantCulture, $"<img alt=\"Logo\" src=\"{InvoiceLogo.DataUri}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<h1>Invoice {html.Encode(order.Id.ToString())}</h1><table>");
        foreach (var line in order.Lines)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<tr><td>{html.Encode(line.Sku)}</td><td>{line.Quantity}</td><td>{line.LineTotal.ToString("C", s_money)}</td></tr>");
        }

        sb.Append(CultureInfo.InvariantCulture, $"</table><p>Total: {order.Total.ToString("C", s_money)}</p>");
        if (order.CustomerNote is { } note)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<p class=\"note\">{html.Encode(note)}</p>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }
}
