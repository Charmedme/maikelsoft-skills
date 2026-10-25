using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShipmentEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Ship_PlacedOrder_MarksOrderShipped()
    {
        var order = await s_client.CreateOrderAsync(Ct);

        var shipment = await s_client.ShipAsync(order.Id, Ct);

        shipment.Carrier.Should().Be("PostNL");
        var after = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", Ct);
        after!.Status.Should().Be("Shipped");
    }

    [TestMethod]
    public async Task Ship_CancelledOrder_ReturnsConflict()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, Ct);

        var response = await s_client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(order.Id, Carrier.Dhl, "JD0123456789"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task Ship_UnknownOrder_ReturnsNotFound()
    {
        var response = await s_client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(Guid.CreateVersion7(), Carrier.Dhl, "JD0123456789"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task MarkDelivered_SetsDeliveredAtOnce()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        var shipment = await s_client.ShipAsync(order.Id, Ct);
        s_factory.Time.Advance(TimeSpan.FromDays(1));
        var deliveredAt = s_factory.Time.GetUtcNow();

        await s_client.PostAsync(new Uri($"/shipments/{shipment.Id}/delivered", UriKind.Relative), null, Ct);
        s_factory.Time.Advance(TimeSpan.FromDays(1));
        var response = await s_client.PostAsync(new Uri($"/shipments/{shipment.Id}/delivered", UriKind.Relative), null, Ct);

        (await response.Content.ReadFromJsonAsync<ShipmentResponse>(Ct))!.DeliveredAt.Should().Be(deliveredAt);
    }
}
