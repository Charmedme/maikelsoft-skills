using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for postgresql-trap. Each test uses its own database in the test container,
// so orders of other tests do not change the counts.
[TestClass]
[DoNotParallelize]
public sealed class PickingProbe
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task TwentyWorkers_TenOrders_EachOrderOnce()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var factory = await StartAsync(time, ct);
        using var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        for (var i = 0; i < 10; i++)
        {
            time.Advance(TimeSpan.FromMinutes(1));
            await PlaceOrderAsync(client, customerId, "SKU-P", 1, 1m, ct);
        }

        var results = new ConcurrentBag<(HttpStatusCode Status, string Body)>();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async worker =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/picking/next");
            request.Headers.Add("X-Worker-Id", $"w-{worker:00}");
            using var response = await client.SendAsync(request, ct);
            results.Add((response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        }));

        var errors = results.Where(r => (int)r.Status >= 300 || (r.Status != HttpStatusCode.NoContent && (int)r.Status < 200)).ToList();
        Assert.IsEmpty(errors, "Unexpected answers: " + string.Join(" | ", errors.Select(e => $"{(int)e.Status} {Short(e.Body)}")));
        var picked = results.Where(r => r.Status != HttpStatusCode.NoContent).Select(r => IdOf(r.Body)).ToList();
        Assert.HasCount(10, picked, $"Expected 10 picked orders and 10 times 204. Got {picked.Count} picked.");
        Assert.HasCount(10, picked.Distinct().ToList(), "Two workers got the same order: " + string.Join(", ", picked.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => $"{g.Key} x{g.Count()}")));

        var orders = await client.GetFromJsonAsync<JsonElement>($"/orders?customerId={customerId}", ct);
        var statuses = orders.EnumerateArray().Select(o => Prop(o, "status").GetString()).ToList();
        Assert.IsTrue(statuses.All(s => s == "Picking"), "After the run, the order statuses are: " + string.Join(", ", statuses));
    }

    [TestMethod]
    public async Task OneWorker_GetsOldestPlacedOrderFirst()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var factory = await StartAsync(time, ct);
        using var client = factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var first = await PlaceOrderAsync(client, customerId, "SKU-O", 1, 1m, ct);
        time.Advance(TimeSpan.FromMinutes(5));
        await PlaceOrderAsync(client, customerId, "SKU-O", 1, 1m, ct);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/picking/next");
        request.Headers.Add("X-Worker-Id", "w-01");
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        Assert.IsTrue(response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent, $"POST /picking/next returned {(int)response.StatusCode}: {Short(body)}");
        Assert.AreEqual(first, IdOf(body), "The first pick did not return the oldest placed order.");
    }

    [TestMethod]
    public async Task DailySales_OrderAt2230Utc_CountsOnNextAmsterdamDay()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 1, 21, 30, 0, TimeSpan.Zero));
        await using var factory = await StartAsync(time, ct);
        using var client = factory.CreateClient();
        await PlaceOrderAsync(client, Guid.CreateVersion7(), "SKU-D", 1, 3m, ct); // 23:30 in Amsterdam, 2026-07-01
        time.Advance(TimeSpan.FromHours(1));
        await PlaceOrderAsync(client, Guid.CreateVersion7(), "SKU-D", 2, 5m, ct); // 00:30 in Amsterdam, 2026-07-02

        using var response = await client.GetAsync(new Uri("/reports/daily-sales?from=2026-07-01&to=2026-07-31", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET /reports/daily-sales returned {(int)response.StatusCode}: {Short(body)}");

        var days = DailySales.Parse(body);
        Assert.AreEqual((1, 3m), days.GetValueOrDefault("2026-07-01"), "Day 2026-07-01 in Amsterdam time. Report: " + Short(body));
        Assert.AreEqual((1, 10m), days.GetValueOrDefault("2026-07-02"), "Day 2026-07-02 in Amsterdam time. Report: " + Short(body));
    }

    private static async Task<WebApplicationFactory<Program>> StartAsync(FakeTimeProvider time, CancellationToken ct)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
        {
            Database = "probe_" + Guid.NewGuid().ToString("N")[..12],
        }.ConnectionString;
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(time);
            });
        });
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        return factory;
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient client, Guid customerId, string sku, int quantity, decimal unitPrice, CancellationToken ct)
    {
        var body = new { customerId, lines = new[] { new { sku, quantity, unitPrice } } };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}: {Short(text)}");
        return IdOf(text);
    }

    private static Guid IdOf(string json) => Prop(JsonDocument.Parse(json).RootElement, "id").GetGuid();

    private static JsonElement Prop(JsonElement element, string name) =>
        element.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string Short(string text) => text.Length > 300 ? text[..300] : text;

    private static class DailySales
    {
        // Reads [{ "day": "2026-07-02", "orders": 1, "revenue": 10.0 }]. Rows with 0 orders are ignored.
        public static Dictionary<string, (int Orders, decimal Revenue)> Parse(string json)
        {
            var result = new Dictionary<string, (int, decimal)>();
            foreach (var row in JsonDocument.Parse(json).RootElement.EnumerateArray())
            {
                var day = Prop(row, "day").GetString()![..10];
                var orders = Prop(row, "orders").GetInt32();
                if (orders > 0)
                {
                    result[day] = (orders, Prop(row, "revenue").GetDecimal());
                }
            }

            return result;
        }
    }
}
