using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Stock;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class StockEndpointsTests
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
    public async Task PlaceOrder_ManagedSku_ReservesStock()
    {
        await s_client.PutAsJsonAsync("/stock/SKU-STOCK-1", new SetStockRequest(10), Ct);

        await s_client.CreateOrderAsync(Ct, sku: "SKU-STOCK-1", quantity: 4);

        var stock = await s_client.GetFromJsonAsync<StockResponse>("/stock/SKU-STOCK-1", Ct);
        stock!.Available.Should().Be(6);
    }

    [TestMethod]
    public async Task PlaceOrder_NotEnoughStock_ReturnsConflictAndKeepsStock()
    {
        await s_client.PutAsJsonAsync("/stock/SKU-STOCK-2", new SetStockRequest(1), Ct);

        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-STOCK-2", 2, 1m)]), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var stock = await s_client.GetFromJsonAsync<StockResponse>("/stock/SKU-STOCK-2", Ct);
        stock!.Available.Should().Be(1);
    }

    [TestMethod]
    public async Task CancelOrder_ReleasesStock()
    {
        await s_client.PutAsJsonAsync("/stock/SKU-STOCK-3", new SetStockRequest(5), Ct);
        var order = await s_client.CreateOrderAsync(Ct, sku: "SKU-STOCK-3", quantity: 5);

        await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, Ct);

        var stock = await s_client.GetFromJsonAsync<StockResponse>("/stock/SKU-STOCK-3", Ct);
        stock!.Available.Should().Be(5);
    }

    [TestMethod]
    public async Task PlaceOrder_UnmanagedSku_HasNoLimit()
    {
        var order = await s_client.CreateOrderAsync(Ct, sku: "SKU-SUPPLIER", quantity: 999);

        order.Status.Should().Be("Placed");
    }

    [TestMethod]
    public async Task SetStock_Twice_KeepsLastValue()
    {
        await s_client.PutAsJsonAsync("/stock/SKU-STOCK-4", new SetStockRequest(3), Ct);
        var response = await s_client.PutAsJsonAsync("/stock/SKU-STOCK-4", new SetStockRequest(8), Ct);

        (await response.Content.ReadFromJsonAsync<StockResponse>(Ct))!.Available.Should().Be(8);
    }
}
