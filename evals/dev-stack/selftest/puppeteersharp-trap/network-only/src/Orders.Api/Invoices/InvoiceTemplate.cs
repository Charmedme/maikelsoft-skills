using System.Globalization;
using System.Text;
using Orders.Api.Orders;

namespace Orders.Api.Invoices;

public static class InvoiceTemplate
{
    public static string Render(Order order)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body><img src=\"https://cdn.example.com/logo.png\">");
        sb.Append(CultureInfo.InvariantCulture, $"<h1>Invoice {order.Id}</h1><table>");
        foreach (var line in order.Lines)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<tr><td>{line.Sku}</td><td>{line.Quantity}</td><td>{line.LineTotal:0.00}</td></tr>");
        }

        sb.Append(CultureInfo.InvariantCulture, $"</table><p>Total: {order.Total:0.00}</p><p>{order.CustomerNote}</p></body></html>");
        return sb.ToString();
    }
}
