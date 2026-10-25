using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Inventory;
using Orders.Api.Orders;
using Orders.Api.Picking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PickingEndpointsTests
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
    public async Task CreateTask_PlacedOrder_ReturnsOpenTaskInDefaultZone()
    {
        var order = await NewOrderAsync();

        var response = await s_client.PostAsJsonAsync("/picking/tasks", new CreatePickTaskRequest(order), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await response.Content.ReadFromJsonAsync<PickTaskResponse>(TestContext.CancellationToken);
        task!.Status.Should().Be("Open");
        task.Zone.Should().Be(PickZone.Default);
    }

    [TestMethod]
    public async Task CreateTask_Twice_ReturnsConflict()
    {
        var order = await NewOrderAsync();
        await s_client.PostAsJsonAsync("/picking/tasks", new CreatePickTaskRequest(order), TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync("/picking/tasks", new CreatePickTaskRequest(order), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task TakeNext_TwoPickersAtOnce_GetDifferentTasks()
    {
        var zone = NewZone();
        for (var i = 0; i < 2; i++)
        {
            var sku = await NewSkuInZoneAsync(zone);
            var order = await NewOrderAsync(sku);
            (await s_client.PostAsJsonAsync("/picking/tasks", new CreatePickTaskRequest(order), TestContext.CancellationToken)).EnsureSuccessStatusCode();
        }

        var responses = await Task.WhenAll(
            s_client.PostAsJsonAsync("/picking/next", new TakeNextPickRequest("picker-1", zone), TestContext.CancellationToken),
            s_client.PostAsJsonAsync("/picking/next", new TakeNextPickRequest("picker-2", zone), TestContext.CancellationToken));

        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        var tasks = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PickTaskResponse>(TestContext.CancellationToken)));
        tasks.Select(t => t!.Id).Should().OnlyHaveUniqueItems();
        tasks.Should().OnlyContain(t => t!.Status == "Picking");
    }

    [TestMethod]
    public async Task TakeNext_EmptyZone_ReturnsNoContent()
    {
        var response = await s_client.PostAsJsonAsync("/picking/next", new TakeNextPickRequest("picker-1", NewZone()), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task ListOpen_Zone_ReturnsOpenTasksOfZone()
    {
        var zone = NewZone();
        var sku = await NewSkuInZoneAsync(zone);
        var order = await NewOrderAsync(sku);
        await s_client.PostAsJsonAsync("/picking/tasks", new CreatePickTaskRequest(order), TestContext.CancellationToken);

        var tasks = await s_client.GetFromJsonAsync<List<PickTaskResponse>>($"/picking?zone={zone}&sort=CreatedAt", TestContext.CancellationToken);

        tasks.Should().ContainSingle().Which.OrderId.Should().Be(order);
    }

    [TestMethod]
    public async Task StockCount_NewAndKnownSku_SetsOnHand()
    {
        var known = "CNT-" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();
        var added = "CNT-" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();
        await s_client.ReceiveStockAsync(known, 4, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync(
            "/inventory/counts",
            new StockCountRequest([new StockCountLine(known, 9), new StockCountLine(added, 3)]),
            TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await s_client.GetFromJsonAsync<StockResponse>($"/inventory/{known}", TestContext.CancellationToken))!.OnHand.Should().Be(9);
        (await s_client.GetFromJsonAsync<StockResponse>($"/inventory/{added}", TestContext.CancellationToken))!.OnHand.Should().Be(3);
    }

    private static string NewZone() => "Z" + Guid.CreateVersion7().ToString("N")[^6..].ToUpperInvariant();

    private async Task<string> NewSkuInZoneAsync(string zone)
    {
        var sku = "PCK-" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();
        (await s_client.PutAsJsonAsync($"/picking/zones/{sku}", new PutPickZoneRequest(zone), TestContext.CancellationToken)).EnsureSuccessStatusCode();
        return sku;
    }

    private async Task<Guid> NewOrderAsync(string sku = "SKU-1")
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        await s_client.ReceiveStockAsync(sku, 1, TestContext.CancellationToken);
        var response = await s_client.PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(sku, 1, 4.50m)]),
            TestContext.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!.Id;
    }
}
