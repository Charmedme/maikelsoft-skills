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
    public async Task CreateOrder_ValidRequest_ReturnsCreatedOrder()
    {
        var customerId = Guid.CreateVersion7();

        var order = await s_client.CreateOrderAsync(Ct, customerId, quantity: 3, unitPrice: 2.50m);

        order.CustomerId.Should().Be(customerId);
        order.Total.Should().Be(7.50m);
        order.Status.Should().Be("Placed");
        order.PlacedAt.Should().Be(s_factory.Time.GetUtcNow());
    }

    [TestMethod]
    public async Task CreateOrder_NoLines_ReturnsValidationProblem()
    {
        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), []), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [TestMethod]
    public async Task GetOrder_Exists_ReturnsOrderWithLines()
    {
        var created = await s_client.CreateOrderAsync(Ct);

        var order = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", Ct);

        order!.Id.Should().Be(created.Id);
        order.Lines.Should().ContainSingle().Which.Sku.Should().Be("SKU-1");
    }

    [TestMethod]
    public async Task GetOrder_Unknown_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task ListOrders_ForCustomer_ReturnsOnlyTheirOrders()
    {
        var customerId = Guid.CreateVersion7();
        await s_client.CreateOrderAsync(Ct, customerId);
        await s_client.CreateOrderAsync(Ct);

        var orders = await s_client.GetFromJsonAsync<List<OrderResponse>>($"/orders?customerId={customerId}", Ct);

        orders.Should().ContainSingle().Which.CustomerId.Should().Be(customerId);
    }

    [TestMethod]
    public async Task CancelOrder_InWindow_ReturnsCancelledOrder()
    {
        var created = await s_client.CreateOrderAsync(Ct);

        var response = await s_client.PostAsync(new Uri($"/orders/{created.Id}/cancel", UriKind.Relative), null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<OrderResponse>(Ct))!.Status.Should().Be("Cancelled");
    }

    [TestMethod]
    public async Task CancelOrder_Shipped_ReturnsConflict()
    {
        var created = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(created.Id, Ct);

        var response = await s_client.PostAsync(new Uri($"/orders/{created.Id}/cancel", UriKind.Relative), null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task HealthReady_DatabaseUp_ReturnsHealthy()
    {
        var response = await s_client.GetAsync(new Uri("/healthz/ready", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
