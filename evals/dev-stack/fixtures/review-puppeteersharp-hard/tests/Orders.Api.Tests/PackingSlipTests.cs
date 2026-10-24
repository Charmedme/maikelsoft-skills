using System.Net;
using System.Text;
using FluentAssertions;
using Orders.Api.Customers;
using Orders.Api.Documents;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PackingSlipTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
        s_client.Timeout = TimeSpan.FromSeconds(120);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public void Template_EncodesDeliveryInstructions()
    {
        var now = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Consumer, null, now);
        var order = new Order(customer.Id == Guid.Empty ? Guid.CreateVersion7() : customer.Id, now, null, "<b>Gate</b>");
        order.AddLine("SKU-1", 1, 1m);
        var shipment = new Shipment(Guid.CreateVersion7(), now);

        var html = PackingSlipTemplate.Render(shipment, order, customer, new DocumentOptions());

        html.Should().Contain("&lt;b&gt;Gate&lt;/b&gt;").And.NotContain("<b>Gate</b>");
    }

    [TestMethod]
    public void Template_BusinessCustomer_ShowsReferenceAndFooter()
    {
        var now = new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Business, "Example Trading B.V.", now);
        customer.SetBranding(null, null, "Goods-in: dock 4");
        var order = new Order(Guid.CreateVersion7(), now, "PO-1001");
        order.AddLine("SKU-1", 1, 1m);
        var shipment = new Shipment(Guid.CreateVersion7(), now);

        var html = PackingSlipTemplate.Render(shipment, order, customer, new DocumentOptions());

        html.Should().Contain("PO-1001").And.Contain("Goods-in: dock 4");
    }

    [TestMethod]
    public async Task PackingSlip_ExistingShipment_ReturnsPdf()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, deliveryInstructions: "Ring twice.");
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.GetAsync(new Uri($"/shipments/{shipment.Id}/packing-slip.pdf", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");
    }

    [TestMethod]
    public async Task PackingSlip_UnknownShipment_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri($"/shipments/{Guid.CreateVersion7()}/packing-slip.pdf", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
