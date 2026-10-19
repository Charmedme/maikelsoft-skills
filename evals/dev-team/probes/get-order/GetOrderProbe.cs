using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-12 (read one order).
[TestClass]
[DoNotParallelize]
public sealed class GetOrderProbe
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
    public async Task Get_ExistingOrder_Returns200WithLines()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(),
            [new CreateOrderLineRequest("SKU-1", 2, 2.50m), new CreateOrderLineRequest("SKU-2", 1, 1m)]);
        var created = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        created.EnsureSuccessStatusCode();
        using var c = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.CancellationToken));
        var id = c.RootElement.GetProperty("id").GetGuid();

        var response = await s_client.GetAsync($"/orders/{id}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual(id, doc.RootElement.GetProperty("id").GetGuid());
        Assert.AreEqual(2, doc.RootElement.GetProperty("lines").GetArrayLength());
        Assert.AreEqual(6.00m, doc.RootElement.GetProperty("total").GetDecimal());
    }

    [TestMethod]
    public async Task Get_UnknownOrder_Returns404()
    {
        var response = await s_client.GetAsync($"/orders/{Guid.CreateVersion7()}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
