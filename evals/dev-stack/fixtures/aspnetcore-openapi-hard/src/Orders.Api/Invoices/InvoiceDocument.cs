using System.Globalization;
using System.Text;
using Orders.Api.Orders;

namespace Orders.Api.Invoices;

// Writes a one-page PDF with the invoice lines. It uses the base font Helvetica, so it needs no font files.
public static class InvoiceDocument
{
    public static byte[] Render(Order order)
    {
        var text = new StringBuilder();
        var y = 800;
        void Line(string value)
        {
            text.Append(CultureInfo.InvariantCulture, $"BT /F1 11 Tf 50 {y} Td ({Escape(value)}) Tj ET\n");
            y -= 16;
        }

        Line($"Invoice {InvoiceNumber.For(order.Id, order.PlacedAt)}");
        Line(string.Create(CultureInfo.InvariantCulture, $"Date {order.PlacedAt:yyyy-MM-dd}"));
        foreach (var line in order.Lines)
        {
            Line(string.Create(CultureInfo.InvariantCulture, $"{line.Sku}  {line.Quantity} x {line.UnitPrice:0.00} = {line.LineTotal:0.00}"));
        }

        Line(string.Create(CultureInfo.InvariantCulture, $"Total {order.Total:0.00}"));
        return Build(text.ToString());
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal)
        .Replace(")", "\\)", StringComparison.Ordinal);

    private static byte[] Build(string content)
    {
        string[] objects =
        [
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            string.Create(CultureInfo.InvariantCulture, $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"),
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
        ];

        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int>();
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            pdf.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
