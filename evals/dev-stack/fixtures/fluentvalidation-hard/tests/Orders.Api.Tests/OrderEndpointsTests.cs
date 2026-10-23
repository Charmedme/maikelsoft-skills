using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
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

        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(customerId, ("SKU-1", 3)), TestContext.CancellationToken);

        order.CustomerId.Should().Be(customerId);
        order.Total.Should().Be(30m);
        order.Status.Should().Be("Placed");
        order.PlacedAt.Should().Be(s_factory.Time.GetUtcNow());
    }

    [TestMethod]
    public async Task CreateOrder_NoLines_ReturnsValidationProblem()
    {
        using var response = await PostOrderAsync(new CreateOrderRequest(Guid.CreateVersion7(), []), Guid.CreateVersion7().ToString("N"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [TestMethod]
    public async Task CreateOrder_WithoutIdempotencyKey_Returns400()
    {
        using var response = await PostOrderAsync(TestData.Order(), key: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeAsync(response)).Should().Be("request.idempotency_key_missing");
    }

    [TestMethod]
    public async Task CreateOrder_SameIdempotencyKeyTwice_SecondReturns409()
    {
        var key = Guid.CreateVersion7().ToString("N");
        using var first = await PostOrderAsync(TestData.Order(), key);
        using var second = await PostOrderAsync(TestData.Order(), key);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeAsync(second)).Should().Be("request.idempotency_key_reused");
    }

    [TestMethod]
    public async Task ListOrders_ForCustomer_ReturnsOnlyTheirOrders()
    {
        var customerId = Guid.CreateVersion7();
        await TestData.PlaceOrderAsync(s_client, TestData.Order(customerId), TestContext.CancellationToken);
        await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);

        var orders = await s_client.GetFromJsonAsync<List<OrderResponse>>($"/orders?customerId={customerId}", TestContext.CancellationToken);

        orders.Should().ContainSingle().Which.CustomerId.Should().Be(customerId);
    }

    [TestMethod]
    public async Task GetOrder_Unknown_Returns404WithCode()
    {
        using var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        (await CodeAsync(response)).Should().Be("order.not_found");
    }

    [TestMethod]
    public async Task CancelOrder_InsideWindow_Returns204()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);

        using var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);
        var after = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        after!.Status.Should().Be("Cancelled");
    }

    [TestMethod]
    public async Task CancelOrder_Shipped_Returns409()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(), TestContext.CancellationToken);
        using var admin = s_factory.CreateAdminClient();
        using var shipped = await admin.PostAsJsonAsync($"/admin/orders/{order.Id}/ship", new { carrier = "PostNL", trackingCode = "3SABC123" }, TestContext.CancellationToken);

        using var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        shipped.StatusCode.Should().Be(HttpStatusCode.OK);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CodeAsync(response)).Should().Be("order.already_shipped");
    }

    private async Task<HttpResponseMessage> PostOrderAsync(CreateOrderRequest request, string? key)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/orders") { Content = JsonContent.Create(request) };
        if (key is not null)
        {
            message.Headers.Add("Idempotency-Key", key);
        }

        return await s_client.SendAsync(message, TestContext.CancellationToken);
    }

    private async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return document.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }
}
