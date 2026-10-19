using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-17 (cancel an order).
// The probe sets the status through the change tracker, not through a domain method,
// because the team can rename or replace the domain methods.
[TestClass]
[DoNotParallelize]
public sealed class CancelOrderProbe
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
    public async Task Cancel_PlacedInsideWindow_Returns200Cancelled()
    {
        var id = await CreateAsync();
        var response = await s_client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("Cancelled", await StatusOfAsync(response));
    }

    [TestMethod]
    public async Task Cancel_Shipped_Returns409Problem()
    {
        var id = await CreateAsync();
        await using (var scope = s_factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.SingleAsync(o => o.Id == id, TestContext.CancellationToken);
            db.Entry(order).Property(nameof(Order.Status)).CurrentValue = OrderStatus.Shipped;
            await db.SaveChangesAsync(TestContext.CancellationToken);
        }

        var response = await s_client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Cancel_WindowClosed_Returns409Problem()
    {
        var id = await CreateAsync();
        // Time only moves forward. Orders that later tests create get the new time as PlacedAt.
        s_factory.Time.Advance(TimeSpan.FromHours(25));
        var response = await s_client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        StringAssert.Contains(body.ToLowerInvariant(), "window");
    }

    [TestMethod]
    public async Task Cancel_Twice_Returns200AndChangesNothing()
    {
        var id = await CreateAsync();
        var first = await s_client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        var firstAt = await CancelledAtOfAsync(first);
        s_factory.Time.Advance(TimeSpan.FromMinutes(5));
        var second = await s_client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
        Assert.AreEqual(firstAt, await CancelledAtOfAsync(second));
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_Returns404()
    {
        var response = await s_client.PostAsync($"/orders/{Guid.CreateVersion7()}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateAsync()
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 2.50m)]);
        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<string?> StatusOfAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return doc.RootElement.GetProperty("status").GetString();
    }

    private async Task<string> CancelledAtOfAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        return doc.RootElement.GetProperty("cancelledAt").GetRawText();
    }
}
