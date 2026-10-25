using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Inventory;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class InventoryEndpointsTests
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
    public async Task Receive_NewSku_CreatesStockItem()
    {
        var sku = NewSku();

        var stock = await s_client.ReceiveStockAsync(sku.ToLowerInvariant(), 7, TestContext.CancellationToken);

        stock.Sku.Should().Be(sku);
        stock.OnHand.Should().Be(7);
        stock.Available.Should().Be(7);
    }

    [TestMethod]
    public async Task Receive_KnownSku_AddsToOnHand()
    {
        var sku = NewSku();
        await s_client.ReceiveStockAsync(sku, 7, TestContext.CancellationToken);

        var stock = await s_client.ReceiveStockAsync(sku, 3, TestContext.CancellationToken);

        stock.OnHand.Should().Be(10);
    }

    [TestMethod]
    public async Task Reserve_EnoughStock_ReducesAvailable()
    {
        var sku = NewSku();
        await s_client.ReceiveStockAsync(sku, 5, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/inventory/{sku}/reservations", new ReserveStockRequest(2), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<StockResponse>(TestContext.CancellationToken))!.Available.Should().Be(3);
    }

    [TestMethod]
    public async Task Reserve_MoreThanAvailable_ReturnsConflict()
    {
        var sku = NewSku();
        await s_client.ReceiveStockAsync(sku, 1, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/inventory/{sku}/reservations", new ReserveStockRequest(2), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task Reserve_UnknownSku_ReturnsNotFound()
    {
        var response = await s_client.PostAsJsonAsync($"/inventory/{NewSku()}/reservations", new ReserveStockRequest(1), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Reserve_TwentyParallelCalls_NeverBelowZero()
    {
        var sku = NewSku();
        await s_client.ReceiveStockAsync(sku, 10, TestContext.CancellationToken);

        var calls = Enumerable.Range(0, 20).Select(_ =>
            s_client.PostAsJsonAsync($"/inventory/{sku}/reservations", new ReserveStockRequest(1), TestContext.CancellationToken));
        var responses = await Task.WhenAll(calls);

        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(10);
        var stock = await s_client.GetFromJsonAsync<StockResponse>($"/inventory/{sku}", TestContext.CancellationToken);
        stock!.Available.Should().Be(0);
    }

    private static string NewSku() => "SKU-" + Guid.CreateVersion7().ToString("N")[^12..].ToUpperInvariant();
}
