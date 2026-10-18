using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-41 (cancel until the end of the next shop day, Europe/Amsterdam).
// Daylight saving time in Europe/Amsterdam ends on 2026-10-25 at 03:00 local time (01:00 UTC).
// Each test uses its own factory, because the fake clock only moves forward.
[TestClass]
[DoNotParallelize]
public sealed class CancelShopDayProbe
{
    public TestContext TestContext { get; set; } = null!;

    private static DateTimeOffset Utc(int day, int hour, int minute) => new(2026, 10, day, hour, minute, 0, TimeSpan.Zero);

    [TestMethod]
    public async Task Cancel_LastHourOfNextShopDay_AfterDstEnd_Returns200()
    {
        // Placed 2026-10-24 23:30 local (CEST). Deadline 2026-10-25 23:59:59 local (CET) = 22:59:59 UTC.
        var code = await CancelAtAsync(placed: Utc(24, 21, 30), cancel: Utc(25, 22, 30));
        Assert.AreEqual(HttpStatusCode.OK, code);
    }

    [TestMethod]
    public async Task Cancel_JustAfterNextShopDay_Returns409()
    {
        // 2026-10-25 23:30 UTC is 2026-10-26 00:30 local: the window is closed.
        var code = await CancelAtAsync(placed: Utc(24, 21, 30), cancel: Utc(25, 23, 30));
        Assert.AreEqual(HttpStatusCode.Conflict, code);
    }

    [TestMethod]
    public async Task Cancel_OrderDayIsShopDateNotUtcDate_Returns200()
    {
        // Placed 2026-10-24 22:30 UTC = 2026-10-25 00:30 local, so the order day is 25 October.
        // Deadline 2026-10-26 23:59:59 local = 22:59:59 UTC.
        var code = await CancelAtAsync(placed: Utc(24, 22, 30), cancel: Utc(26, 22, 0));
        Assert.AreEqual(HttpStatusCode.OK, code);
    }

    [TestMethod]
    public async Task Cancel_Shipped_Returns409Problem()
    {
        await using var factory = await StartAsync(Utc(16, 9, 0));
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var order = await db.Orders.SingleAsync(o => o.Id == id, TestContext.CancellationToken);
            db.Entry(order).Property(nameof(Order.Status)).CurrentValue = OrderStatus.Shipped;
            await db.SaveChangesAsync(TestContext.CancellationToken);
        }

        var response = await client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task Cancel_Twice_Returns200AndChangesNothing()
    {
        await using var factory = await StartAsync(Utc(16, 9, 0));
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        var first = await client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        var firstAt = await FieldAsync(first, "cancelledAt");
        factory.Time.Advance(TimeSpan.FromMinutes(5));
        var second = await client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
        Assert.AreEqual(firstAt, await FieldAsync(second, "cancelledAt"));
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_Returns404()
    {
        await using var factory = await StartAsync(Utc(16, 9, 0));
        using var client = factory.CreateClient();
        var response = await client.PostAsync($"/orders/{Guid.CreateVersion7()}/cancel", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpStatusCode> CancelAtAsync(DateTimeOffset placed, DateTimeOffset cancel)
    {
        await using var factory = await StartAsync(placed);
        using var client = factory.CreateClient();
        var id = await CreateAsync(client);
        factory.Time.SetUtcNow(cancel);
        var response = await client.PostAsync($"/orders/{id}/cancel", null, TestContext.CancellationToken);
        return response.StatusCode;
    }

    private async Task<OrdersApiFactory> StartAsync(DateTimeOffset now)
    {
        var factory = new OrdersApiFactory();
        if (now > factory.Time.GetUtcNow())
        {
            factory.Time.SetUtcNow(now);
        }

        await factory.MigrateAsync(TestContext.CancellationToken);
        return factory;
    }

    private async Task<Guid> CreateAsync(HttpClient client)
    {
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest("SKU-1", 1, 2.50m)]);
        var response = await client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        return Guid.Parse(await FieldAsync(response, "id"));
    }

    private async Task<string> FieldAsync(HttpResponseMessage response, string name)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
        var value = doc.RootElement.GetProperty(name);
        return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();
    }
}
