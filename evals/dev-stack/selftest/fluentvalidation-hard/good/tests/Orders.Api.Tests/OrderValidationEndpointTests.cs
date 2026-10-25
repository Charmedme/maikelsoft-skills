using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderValidationEndpointTests
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
    public async Task CreateOrder_InactiveProduct_Returns400WithCode()
    {
        var codes = await RejectedCodesAsync(TestData.Order(null, ("SKU-OLD", 1)));

        codes.Should().ContainKey("Lines[0].Sku").WhoseValue.Should().Equal("order.sku_unknown");
    }

    [TestMethod]
    public async Task CreateOrder_SameProductTwice_Returns400WithCode()
    {
        var codes = await RejectedCodesAsync(TestData.Order(null, ("SKU-1", 1), ("sku-1", 2)));

        codes.Should().ContainKey("Lines").WhoseValue.Should().Equal("order.sku_duplicate");
    }

    private async Task<Dictionary<string, string[]>> RejectedCodesAsync(CreateOrderRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/orders") { Content = JsonContent.Create(request) };
        message.Headers.Add("Idempotency-Key", Guid.CreateVersion7().ToString("N"));
        using var response = await s_client.SendAsync(message, TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, body);
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("code").GetString().Should().Be("validation.failed");
        return document.RootElement.GetProperty("errorCodes").Deserialize<Dictionary<string, string[]>>()!;
    }
}
