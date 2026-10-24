using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team bug ORD-45 (the order list accepts an empty customer id).
[TestClass]
[DoNotParallelize]
public sealed class ListEmptyCustomerProbe
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
    public async Task List_EmptyCustomerId_Returns400NamingTheField()
    {
        var response = await s_client.GetAsync($"/orders?customerId={Guid.Empty}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        using var doc = JsonDocument.Parse(body);
        Assert.IsTrue(doc.RootElement.TryGetProperty("errors", out var errors), body);
        Assert.IsTrue(errors.EnumerateObject().Any(e => e.Name.Equals("customerId", StringComparison.OrdinalIgnoreCase)), body);
    }

    [TestMethod]
    public async Task List_KnownCustomer_StillReturns200WithOrders()
    {
        var customer = Guid.CreateVersion7();
        var created = await s_client.PostAsJsonAsync("/orders",
            new CreateOrderRequest(customer, [new CreateOrderLineRequest("SKU-1", 1, 1m)]), TestContext.CancellationToken);
        created.EnsureSuccessStatusCode();

        var response = await s_client.GetAsync($"/orders?customerId={customer}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        Assert.AreEqual(1, doc.RootElement.GetArrayLength());
    }
}
