using FluentAssertions;
using Orders.Api.Invoices;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
public sealed class InvoiceTemplateTests
{
    [TestMethod]
    public void Render_ShowsLinesAndNote()
    {
        var order = new Order(Guid.CreateVersion7(), new DateTimeOffset(2026, 10, 19, 9, 0, 0, TimeSpan.Zero), "Leave at the door");
        order.AddLine("SKU-1", 2, 3.50m);

        var html = InvoiceTemplate.Render(order);

        html.Should().Contain("SKU-1").And.Contain("7.00").And.Contain("Leave at the door");
    }
}
