using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderEndpointsTests
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
    public async Task CreateOrder_ValidRequest_ReturnsCreatedOrder()
    {
        var customerId = Guid.CreateVersion7();
        var request = new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 3, 2.50m)], "Ann Example", "ann@example.com");

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        order!.CustomerId.Should().Be(customerId);
        order.Total.Should().Be(7.50m);
        order.Status.Should().Be("Placed");
        order.PlacedAt.Should().Be(s_factory.Time.GetUtcNow());
    }

    [TestMethod]
    public async Task CreateOrder_NoLines_ReturnsValidationProblem()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [], "Ann Example", "ann@example.com");

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [TestMethod]
    public async Task ListOrders_ForCustomer_ReturnsOnlyTheirOrders()
    {
        var customerId = Guid.CreateVersion7();
        await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 1, 1m)], "Ann Example", "ann@example.com"), TestContext.CancellationToken);
        await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-2", 1, 1m)], "Bob Example", "bob@example.com"), TestContext.CancellationToken);

        var orders = await s_client.GetFromJsonAsync<List<OrderResponse>>($"/orders?customerId={customerId}", TestContext.CancellationToken);

        orders.Should().ContainSingle().Which.CustomerId.Should().Be(customerId);
    }

    [TestMethod]
    public async Task HealthReady_DatabaseUp_ReturnsHealthy()
    {
        var response = await s_client.GetAsync(new Uri("/healthz/ready", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
