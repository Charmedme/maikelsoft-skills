using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderCacheTests
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
    public async Task GetById_ExistingOrder_ReturnsOk()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 2m)]);
        var created = await (await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken))
            .Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);

        var response = await s_client.GetAsync(new Uri($"/orders/{created!.Id}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_ReturnsNotFound()
    {
        var response = await s_client.PostAsync(new Uri($"/orders/{Guid.CreateVersion7()}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
