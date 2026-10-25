using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for postgresql-hard. Each test uses its own database in the test container.
[TestClass]
[DoNotParallelize]
public sealed partial class PgHardProbe
{
    private static readonly string[] s_liveTables = ["orders", "order_lines", "shipments"];

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task TwentyWorkers_TenOrders_EachOrderOnce()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, ct);
        using var client = app.Factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        for (var i = 0; i < 10; i++)
        {
            time.Advance(TimeSpan.FromMinutes(1));
            await PlaceOrderAsync(client, customerId, express: i % 3 == 0, ct);
        }

        var results = new ConcurrentBag<(HttpStatusCode Status, string Body)>();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async worker =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/picking/next");
            request.Headers.Add("X-Worker-Id", $"w-{worker:00}");
            using var response = await client.SendAsync(request, ct);
            results.Add((response.StatusCode, await response.Content.ReadAsStringAsync(ct)));
        }));

        var errors = results.Where(r => r.Status != HttpStatusCode.OK && r.Status != HttpStatusCode.NoContent).ToList();
        Assert.IsEmpty(errors, "Unexpected answers: " + string.Join(" | ", errors.Select(e => $"{(int)e.Status} {Short(e.Body)}")));
        var picked = results.Where(r => r.Status == HttpStatusCode.OK).Select(r => IdOf(r.Body)).ToList();
        Assert.HasCount(10, picked.Distinct().ToList(), "Each of the 10 orders must go to exactly one worker. Picked: " + string.Join(", ", picked.GroupBy(id => id).Select(g => $"{g.Key} x{g.Count()}")));
        Assert.HasCount(10, picked, "Two workers got the same order.");
    }

    [TestMethod]
    public async Task Picking_ExpressOrderBeforeOlderNormalOrder()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, ct);
        using var client = app.Factory.CreateClient();
        var customerId = Guid.CreateVersion7();
        var normalOld = await PlaceOrderAsync(client, customerId, express: false, ct);
        time.Advance(TimeSpan.FromMinutes(5));
        var expressOld = await PlaceOrderAsync(client, customerId, express: true, ct);
        time.Advance(TimeSpan.FromMinutes(5));
        var expressNew = await PlaceOrderAsync(client, customerId, express: true, ct);

        var order = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/picking/next");
            request.Headers.Add("X-Worker-Id", "w-01");
            using var response = await client.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"POST /picking/next returned {(int)response.StatusCode}: {Short(body)}");
            order.Add(IdOf(body));
        }

        CollectionAssert.AreEqual(new[] { expressOld, expressNew, normalOld }, order, "Express orders first, then normal orders, each group oldest first.");
    }

    [TestMethod]
    public async Task DailySales_OrderAt2230Utc_CountsOnNextAmsterdamDay()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 7, 1, 21, 30, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, ct);
        using var client = app.Factory.CreateClient();
        await PlaceOrderAsync(client, Guid.CreateVersion7(), false, ct, quantity: 1, unitPrice: 3m); // 23:30 in Amsterdam, 2026-07-01
        time.Advance(TimeSpan.FromHours(1));
        await PlaceOrderAsync(client, Guid.CreateVersion7(), false, ct, quantity: 2, unitPrice: 5m); // 00:30 in Amsterdam, 2026-07-02

        var rows = await ReportAsync(client, "/reports/daily-sales?from=2026-07-01&to=2026-07-31", "day", ct);
        Assert.AreEqual((1, 3m), rows.GetValueOrDefault("2026-07-01"), "Day 2026-07-01 in Amsterdam time. Report: " + Describe(rows));
        Assert.AreEqual((1, 10m), rows.GetValueOrDefault("2026-07-02"), "Day 2026-07-02 in Amsterdam time. Report: " + Describe(rows));
    }

    [TestMethod]
    public async Task WeeklySales_SundayEveningUtc_CountsInNextAmsterdamWeek()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 6, 3, 10, 0, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, ct);
        using var client = app.Factory.CreateClient();
        await PlaceOrderAsync(client, Guid.CreateVersion7(), false, ct, quantity: 1, unitPrice: 4m); // Wednesday 2026-06-03
        time.SetUtcNow(new DateTimeOffset(2026, 6, 7, 21, 30, 0, TimeSpan.Zero));
        await PlaceOrderAsync(client, Guid.CreateVersion7(), false, ct, quantity: 1, unitPrice: 6m); // Sunday 23:30 in Amsterdam
        time.Advance(TimeSpan.FromHours(1));
        await PlaceOrderAsync(client, Guid.CreateVersion7(), false, ct, quantity: 1, unitPrice: 7m); // Monday 00:30 in Amsterdam

        var rows = await ReportAsync(client, "/reports/weekly-sales?from=2026-06-01&to=2026-06-30", "weekStart", ct);
        Assert.AreEqual((2, 10m), rows.GetValueOrDefault("2026-06-01"), "Week of Monday 2026-06-01 in Amsterdam time. Report: " + Describe(rows));
        Assert.AreEqual((1, 7m), rows.GetValueOrDefault("2026-06-08"), "Week of Monday 2026-06-08 in Amsterdam time. Report: " + Describe(rows));
    }

    [TestMethod]
    public async Task Migration_ExistingOrders_GetDistinctTrackingCodes()
    {
        var ct = TestContext.CancellationToken;
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, ct, migrate: false);
        string first;
        await using (var scope = app.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            first = db.Database.GetMigrations().First();
            await db.GetService<IMigrator>().MigrateAsync(first, ct);
        }

        var ids = new List<Guid>();
        await using (var connection = new NpgsqlConnection(app.ConnectionString))
        {
            await connection.OpenAsync(ct);
            for (var i = 0; i < 3; i++)
            {
                var id = Guid.CreateVersion7();
                ids.Add(id);
                await using var insert = new NpgsqlCommand(
                    """
                    INSERT INTO orders ("Id", "CustomerId", "Status", "PlacedAt", "DiscountPercent") VALUES (@id, @customer, 'Shipped', @at, 0);
                    INSERT INTO order_lines ("OrderId", "Sku", "Quantity", "UnitPrice") VALUES (@id, 'OLD-1', 1, 2.5);
                    """,
                    connection);
                insert.Parameters.AddWithValue("id", id);
                insert.Parameters.AddWithValue("customer", Guid.CreateVersion7());
                insert.Parameters.AddWithValue("at", new DateTimeOffset(2026, 9, 1, 12, i, 0, TimeSpan.Zero));
                await insert.ExecuteNonQueryAsync(ct);
            }
        }

        await using (var scope = app.Factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        }

        using var client = app.Factory.CreateClient();
        var codes = new List<string>();
        foreach (var id in ids)
        {
            var order = await client.GetFromJsonAsync<JsonElement>($"/orders/{id}", ct);
            var code = TryProp(order, "trackingCode");
            Assert.IsFalse(string.IsNullOrWhiteSpace(code) || code.Trim('0', '-').Length == 0, $"Old order {id} has no tracking code: {Short(order.GetRawText())}");
            codes.Add(code!);
            using var track = await client.GetAsync(new Uri($"/track/{Uri.EscapeDataString(code!)}", UriKind.Relative), ct);
            Assert.AreEqual(HttpStatusCode.OK, track.StatusCode, $"GET /track/{code} for an old order.");
        }

        Assert.HasCount(3, codes.Distinct().ToList(), "Old orders share a tracking code: " + string.Join(", ", codes));
    }

    [TestMethod]
    public async Task MigrationScript_NoTableRewriteOnLiveTables()
    {
        var script = await ScriptAfterFixtureAsync();
        var bad = Statements(script)
            .Where(s => LiveTable(s) && AddColumnRx().IsMatch(s) && VolatileDefaultRx().IsMatch(s))
            .ToList();
        Assert.IsEmpty(bad, "ADD COLUMN with a volatile default writes the whole table again under an ACCESS EXCLUSIVE lock: " + string.Join(" | ", bad.Select(Short)));
    }

    [TestMethod]
    public async Task MigrationScript_IndexesOnLiveTablesAreConcurrent()
    {
        var script = await ScriptAfterFixtureAsync();
        var bad = Statements(script)
            .Where(s => LiveTable(s) && CreateIndexRx().IsMatch(s) && !s.Contains("CONCURRENTLY", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.IsEmpty(bad, "CREATE INDEX without CONCURRENTLY blocks writes on a live table: " + string.Join(" | ", bad.Select(Short)));
    }

    private async Task<string> ScriptAfterFixtureAsync()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await using var app = await StartAsync(time, TestContext.CancellationToken, migrate: false);
        await using var scope = app.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var migrations = db.Database.GetMigrations().ToList();
        Assert.IsGreaterThan(1, migrations.Count, "No new migration.");
        return db.GetService<IMigrator>().GenerateScript(fromMigration: migrations[0]);
    }

    private static IEnumerable<string> Statements(string script) =>
        script.Split(';').Select(s => Regex.Replace(s, @"\s+", " ").Trim()).Where(s => s.Length > 0);

    private static bool LiveTable(string statement) =>
        s_liveTables.Any(t => Regex.IsMatch(statement, $@"\b(TABLE|ON)\s+(ONLY\s+)?(public\.)?""?{t}""?(\s|\(|$)", RegexOptions.IgnoreCase));

    [GeneratedRegex(@"\bADD\s+(COLUMN\s+)?", RegexOptions.IgnoreCase)]
    private static partial Regex AddColumnRx();

    [GeneratedRegex(@"\bDEFAULT\b.*\b(gen_random_uuid|uuidv4|uuidv7|uuid_generate_v\d|random|gen_random_bytes|clock_timestamp|timeofday|nextval)\s*\(", RegexOptions.IgnoreCase)]
    private static partial Regex VolatileDefaultRx();

    [GeneratedRegex(@"\bCREATE\s+(UNIQUE\s+)?INDEX\b", RegexOptions.IgnoreCase)]
    private static partial Regex CreateIndexRx();

    private sealed record App(WebApplicationFactory<Program> Factory, string ConnectionString) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Factory.DisposeAsync();
    }

    private static async Task<App> StartAsync(FakeTimeProvider time, CancellationToken ct, bool migrate = true)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
        {
            Database = "probe_" + Guid.NewGuid().ToString("N")[..12],
        }.ConnectionString;
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", connectionString);
            builder.UseSetting("Returns:ExpiryJobEnabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(time);
            });
        });
        if (migrate)
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        }

        return new App(factory, connectionString);
    }

    private static async Task<Guid> PlaceOrderAsync(HttpClient client, Guid customerId, bool express, CancellationToken ct, int quantity = 1, decimal unitPrice = 1m)
    {
        var body = new { customerId, express, lines = new[] { new { sku = "SKU-P", quantity, unitPrice } } };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}: {Short(text)}");
        return IdOf(text);
    }

    private static async Task<Dictionary<string, (int Orders, decimal Revenue)>> ReportAsync(HttpClient client, string url, string keyName, CancellationToken ct)
    {
        using var response = await client.GetAsync(new Uri(url, UriKind.Relative), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET {url} returned {(int)response.StatusCode}: {Short(body)}");
        var result = new Dictionary<string, (int, decimal)>();
        foreach (var row in JsonDocument.Parse(body).RootElement.EnumerateArray())
        {
            var orders = Prop(row, "orders").GetInt32();
            if (orders > 0)
            {
                result[Prop(row, keyName).GetString()![..10]] = (orders, Prop(row, "revenue").GetDecimal());
            }
        }

        return result;
    }

    private static string Describe(Dictionary<string, (int Orders, decimal Revenue)> rows) =>
        string.Join(", ", rows.OrderBy(r => r.Key, StringComparer.Ordinal).Select(r => $"{r.Key}: {r.Value.Orders} / {r.Value.Revenue}"));

    private static Guid IdOf(string json) => Prop(JsonDocument.Parse(json).RootElement, "id").GetGuid();

    private static string? TryProp(JsonElement element, string name)
    {
        foreach (var p in element.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value.ValueKind == JsonValueKind.Null ? null : p.Value.ToString();
            }
        }

        return null;
    }

    private static JsonElement Prop(JsonElement element, string name) =>
        element.EnumerateObject().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string Short(string text) => text.Length > 300 ? text[..300] : text;
}
