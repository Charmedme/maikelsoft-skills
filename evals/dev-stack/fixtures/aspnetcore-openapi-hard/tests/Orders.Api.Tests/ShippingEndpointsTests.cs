using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShippingEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;
    private static HttpClient s_admin = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_client = s_factory.CreateClient();
        s_admin = s_factory.CreateAdminClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        s_admin.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Ship_PlacedOrder_CustomerSeesTrackingLink()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);

        using var shipped = await s_admin.PostAsJsonAsync($"/admin/orders/{order.Id}/ship", new ShipOrderRequest("PostNL", "3SXYZ987"), TestContext.CancellationToken);
        var shipment = await s_client.GetFromJsonAsync<ShipmentResponse>($"/orders/{order.Id}/shipment", TestContext.CancellationToken);

        shipped.StatusCode.Should().Be(HttpStatusCode.OK);
        shipment!.TrackingUrl.AbsoluteUri.Should().StartWith("https://jouw.postnl.nl/");
    }

    [TestMethod]
    public async Task Ship_Twice_SecondReturns409()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);
        using var first = await s_admin.PostAsJsonAsync($"/admin/orders/{order.Id}/ship", new ShipOrderRequest("DHL", "JVGL1234"), TestContext.CancellationToken);

        using var second = await s_admin.PostAsJsonAsync($"/admin/orders/{order.Id}/ship", new ShipOrderRequest("DHL", "JVGL1234"), TestContext.CancellationToken);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task GetShipment_NotShipped_Returns404()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);

        using var response = await s_client.GetAsync(new Uri($"/orders/{order.Id}/shipment", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
