using System.Text;
using FluentAssertions;
using Orders.Api.Invoices;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class InvoiceDocumentTests
{
    private static readonly DateTimeOffset s_placedAt = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void InvoiceNumber_UsesYearAndOrderId()
    {
        var id = Guid.Parse("1a2b3c4d-0000-7000-8000-000000000000");

        InvoiceNumber.For(id, s_placedAt).Should().Be("INV-2026-1A2B3C4D");
    }

    [TestMethod]
    public void Render_ContainsLinesAndTotal()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.AddLine("SKU-1", 2, 18.50m);

        var text = Encoding.ASCII.GetString(InvoiceDocument.Render(order));

        text.Should().StartWith("%PDF-1.4").And.Contain("(SKU-1  2 x 18.50 = 37.00)").And.Contain("(Total 37.00)").And.EndWith("%%EOF\n");
    }

    [TestMethod]
    public void Render_EscapesParentheses()
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.AddLine("A(1)", 1, 1m);

        Encoding.ASCII.GetString(InvoiceDocument.Render(order)).Should().Contain("A\\(1\\)");
    }
}
