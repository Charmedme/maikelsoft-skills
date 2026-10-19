using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-40 (ship an order and show its history).
[TestClass]
[DoNotParallelize]
public sealed class ShipHistoryProbe
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
    public async Task Ship_PlacedOrder_Returns200WithCarrierAndTrackingCode()
    {
        var (id, _) = await CreateAsync();
        var response = await ShipAsync(id, "DHL", "JD014600006751");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var doc = await JsonAsync(response);
        Assert.AreEqual("Shipped", doc.RootElement.GetProperty("status").GetString());
        Assert.AreEqual("DHL", doc.RootElement.GetProperty("carrier").GetString());
        Assert.AreEqual("JD014600006751", doc.RootElement.GetProperty("trackingCode").GetString());
    }

    [TestMethod]
    public async Task Ship_UnknownCarrier_Returns400()
    {
        var (id, _) = await CreateAsync();
        var response = await ShipAsync(id, "FedEx", "JD014600006751");
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Ship_BadTrackingCode_Returns400()
    {
        var (id, _) = await CreateAsync();
        Assert.AreEqual(HttpStatusCode.BadRequest, (await ShipAsync(id, "UPS", "ABC123")).StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await ShipAsync(id, "UPS", "ABC-1234-XYZ")).StatusCode);
    }

    [TestMethod]
    public async Task Ship_Twice_Returns409Problem()
    {
        var (id, _) = await CreateAsync();
        Assert.AreEqual(HttpStatusCode.OK, (await ShipAsync(id, "PostNL", "3SABCD1234567")).StatusCode);
        var second = await ShipAsync(id, "PostNL", "3SABCD1234567");
        Assert.AreEqual(HttpStatusCode.Conflict, second.StatusCode);
        Assert.AreEqual("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task History_PlacedThenShipped_OldestFirst()
    {
        var (id, _) = await CreateAsync();
        s_factory.Time.Advance(TimeSpan.FromHours(2));
        Assert.AreEqual(HttpStatusCode.OK, (await ShipAsync(id, "DHL", "JD014600006751")).StatusCode);
        var response = await s_client.GetAsync($"/orders/{id}/history", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var doc = await JsonAsync(response);
        var events = Events(doc.RootElement).ToList();
        CollectionAssert.AreEqual(new[] { "Placed", "Shipped" }, events.Select(e => e.Type).ToArray());
        Assert.IsTrue(events[0].At < events[1].At);
    }

    [TestMethod]
    public async Task List_StatusFilter_ReturnsOnlyThatStatus()
    {
        var customer = Guid.CreateVersion7();
        var (shipped, _) = await CreateAsync(customer);
        await CreateAsync(customer);
        Assert.AreEqual(HttpStatusCode.OK, (await ShipAsync(shipped, "UPS", "1Z999AA10123456784")).StatusCode);

        using var filtered = await JsonAsync(await s_client.GetAsync($"/orders?customerId={customer}&status=Shipped", TestContext.CancellationToken));
        Assert.AreEqual(1, filtered.RootElement.GetArrayLength());
        Assert.AreEqual(shipped, filtered.RootElement[0].GetProperty("id").GetGuid());

        using var all = await JsonAsync(await s_client.GetAsync($"/orders?customerId={customer}", TestContext.CancellationToken));
        Assert.AreEqual(2, all.RootElement.GetArrayLength());
    }

    [TestMethod]
    public async Task UnknownOrder_Returns404OnShipAndHistory()
    {
        var id = Guid.CreateVersion7();
        Assert.AreEqual(HttpStatusCode.NotFound, (await ShipAsync(id, "DHL", "JD014600006751")).StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, (await s_client.GetAsync($"/orders/{id}/history", TestContext.CancellationToken)).StatusCode);
    }

    private static IEnumerable<(string? Type, DateTimeOffset At)> Events(JsonElement root)
    {
        var list = root.ValueKind == JsonValueKind.Array ? root : root.GetProperty("events");
        foreach (var e in list.EnumerateArray())
        {
            yield return (e.GetProperty("type").GetString(), e.GetProperty("occurredAt").GetDateTimeOffset());
        }
    }

    private async Task<(Guid Id, Guid Customer)> CreateAsync(Guid? customer = null)
    {
        var customerId = customer ?? Guid.CreateVersion7();
        var request = new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 1, 2.50m)]);
        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = await JsonAsync(response);
        return (doc.RootElement.GetProperty("id").GetGuid(), customerId);
    }

    private Task<HttpResponseMessage> ShipAsync(Guid id, string carrier, string trackingCode) =>
        s_client.PostAsJsonAsync($"/orders/{id}/ship", new { carrier, trackingCode }, TestContext.CancellationToken);

    private async Task<JsonDocument> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
}
