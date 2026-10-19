using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrdersApiVersionTests
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
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task GetOrderV2_ExistingOrder_ReturnsOrder()
    {
        var created = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 5m)]), TestContext.CancellationToken);
        var order = await created.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);

        var response = await s_client.GetAsync(new Uri($"/api/v2/orders/{order!.Id}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        body!.CreatedAt.Should().Be(s_factory.Time.GetUtcNow());
    }

    [TestMethod]
    public async Task GetOrderV1_UnknownOrder_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri($"/api/v1/orders/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
