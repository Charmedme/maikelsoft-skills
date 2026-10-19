using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for sqlserver-trap. Each test uses its own database in the test container,
// so rows of other tests do not change the counts. The probe reads dbo.StockLevels (Sku, Available)
// and dbo.Orders directly, because the request names that table and its columns.
[TestClass]
[DoNotParallelize]
public sealed class StockProbe
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task FiftyParallelReservations_NeverBelowZero()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await ProbeApp.StartAsync(new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero)), ct);
        using var client = app.Factory.CreateClient();
        await PutStockAsync(client, "SKU-R", 10, ct);

        var results = new ConcurrentBag<(HttpStatusCode Status, string Body)>();
        await Task.WhenAll(Enumerable.Range(0, 50).Select(async _ =>
        {
            var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku = "SKU-R", quantity = 1, unitPrice = 1m } } };
            using var response = await client.PostAsJsonAsync("/orders", body, ct);
            results.Add((response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        }));

        var errors = results.Where(r => !r.Status.IsSuccess() && r.Status != HttpStatusCode.Conflict).ToList();
        Assert.IsEmpty(errors, "Answers other than 2xx and 409: " + string.Join(" | ", errors.Take(3).Select(e => $"{(int)e.Status} {Short(e.Body)}")));
        var created = results.Count(r => r.Status.IsSuccess());
        var available = await app.ScalarAsync<int>("SELECT Available FROM dbo.StockLevels WHERE Sku = 'SKU-R'", ct);
        var orders = await app.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders", ct);
        Assert.IsGreaterThanOrEqualTo(0, available, $"Available is {available}.");
        Assert.AreEqual(10, created + available, $"{created} orders were created and {available} units are left. The stock was 10, so stock was oversold or lost.");
        Assert.AreEqual(created, orders, $"{created} answers were 2xx, but the database has {orders} orders.");
        Assert.IsGreaterThan(0, created, "No order was created.");
    }

    [TestMethod]
    public async Task TwoLines_SecondShort_NothingReserved()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await ProbeApp.StartAsync(new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero)), ct);
        using var client = app.Factory.CreateClient();
        await PutStockAsync(client, "SKU-A", 5, ct);
        await PutStockAsync(client, "SKU-B", 0, ct);

        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku = "SKU-A", quantity = 2, unitPrice = 1m }, new { sku = "SKU-B", quantity = 1, unitPrice = 1m } } };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, Short(await response.Content.ReadAsStringAsync(ct)));
        Assert.AreEqual(5, await app.ScalarAsync<int>("SELECT Available FROM dbo.StockLevels WHERE Sku = 'SKU-A'", ct), "SKU-A lost stock for an order that was refused.");
        Assert.AreEqual(0, await app.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders", ct), "A refused order is in the database.");
    }

    [TestMethod]
    public async Task ParallelPutSameSku_NoServerError()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await ProbeApp.StartAsync(new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero)), ct);
        using var client = app.Factory.CreateClient();

        var failures = new ConcurrentBag<string>();
        for (var round = 0; round < 60; round++)
        {
            var sku = $"SKU-U{round:00}";
            await Task.WhenAll(Enumerable.Range(1, 4).Select(async value =>
            {
                using var response = await client.PutAsJsonAsync($"/stock/{sku}", new { available = value }, ct);
                if (!response.IsSuccessStatusCode)
                {
                    failures.Add($"{sku}: {(int)response.StatusCode} {Short(await response.Content.ReadAsStringAsync(ct))}");
                }
            }));
        }

        Assert.IsEmpty(failures, $"{failures.Count} of 240 PUT calls failed: " + string.Join(" | ", failures.Take(3)));
        Assert.AreEqual(60, await app.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.StockLevels WHERE Sku LIKE 'SKU-U%'", ct), "Expected one row per SKU.");
    }

    [TestMethod]
    public async Task DailySales_OrderAt2230Utc_CountsOnNextAmsterdamDay()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 1, 21, 30, 0, TimeSpan.Zero));
        await using var app = await ProbeApp.StartAsync(time, ct);
        using var client = app.Factory.CreateClient();
        await PutStockAsync(client, "SKU-D", 100, ct);
        await PlaceOrderAsync(client, "SKU-D", 1, 3m, ct); // 23:30 in Amsterdam, 2026-07-01
        time.Advance(TimeSpan.FromHours(1));
        await PlaceOrderAsync(client, "SKU-D", 2, 5m, ct); // 00:30 in Amsterdam, 2026-07-02

        using var response = await client.GetAsync(new Uri("/reports/daily-sales?from=2026-07-01&to=2026-07-31", UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET /reports/daily-sales returned {(int)response.StatusCode}: {Short(body)}");

        var days = new Dictionary<string, (int, decimal)>();
        foreach (var row in JsonDocument.Parse(body).RootElement.EnumerateArray())
        {
            var orders = Prop(row, "orders").GetInt32();
            if (orders > 0)
            {
                days[Prop(row, "day").GetString()![..10]] = (orders, Prop(row, "revenue").GetDecimal());
            }
        }

        Assert.AreEqual((1, 3m), days.GetValueOrDefault("2026-07-01"), "Day 2026-07-01 in Amsterdam time. Report: " + Short(body));
        Assert.AreEqual((1, 10m), days.GetValueOrDefault("2026-07-02"), "Day 2026-07-02 in Amsterdam time. Report: " + Short(body));
    }

    private static async Task PutStockAsync(HttpClient client, string sku, int available, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync($"/stock/{sku}", new { available }, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT /stock/{sku} returned {(int)response.StatusCode}: {Short(await response.Content.ReadAsStringAsync(ct))}");
    }

    private static async Task PlaceOrderAsync(HttpClient client, string sku, int quantity, decimal unitPrice, CancellationToken ct)
    {
        var body = new { customerId = Guid.CreateVersion7(), lines = new[] { new { sku, quantity, unitPrice } } };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}: {Short(await response.Content.ReadAsStringAsync(ct))}");
    }

    private static JsonElement Prop(JsonElement element, string name) =>
        element.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string Short(string text) => text.Length > 300 ? text[..300] : text;

    private sealed class ProbeApp : IAsyncDisposable
    {
        private ProbeApp(WebApplicationFactory<Program> factory, string connectionString)
        {
            Factory = factory;
            ConnectionString = connectionString;
        }

        public WebApplicationFactory<Program> Factory { get; }

        public string ConnectionString { get; }

        public static async Task<ProbeApp> StartAsync(FakeTimeProvider time, CancellationToken ct)
        {
            var connectionString = new SqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
            {
                InitialCatalog = "probe_" + Guid.NewGuid().ToString("N")[..12],
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
            return new ProbeApp(factory, connectionString);
        }

        public async Task<T> ScalarAsync<T>(string sql, CancellationToken ct)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand(sql, connection);
            return (T)Convert.ChangeType((await command.ExecuteScalarAsync(ct))!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
        }

        public async ValueTask DisposeAsync() => await Factory.DisposeAsync();
    }
}

internal static class StatusCodeExtensions
{
    public static bool IsSuccess(this HttpStatusCode status) => (int)status is >= 200 and < 300;
}
