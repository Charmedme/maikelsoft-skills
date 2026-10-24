using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for sqlserver-hard. It uses only the public HTTP API, on a database of its own
// in the SQL Server container of the test assembly, so that the plan cache and the report
// counts hold only the work of this class.
[TestClass]
[DoNotParallelize]
public sealed class VoucherProbe
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);
    private static readonly Regex s_columnConversion = new(
        @"CONVERT_IMPLICIT\(nvarchar\(\d+\),\[[^\]]+\]\.\[dbo\]\.\[(?!__EFMigrationsHistory)[^\]]+\]\.\[[^\]]+\]",
        RegexOptions.CultureInvariant);
    private static ProbeFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    private static string ConnectionString =>
        new SqlConnectionStringBuilder(AssemblyFixture.ConnectionString) { InitialCatalog = "SqlServerHardProbe" }.ConnectionString;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new ProbeFactory();
        await using (var scope = s_factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(context.CancellationToken);
        }

        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Voucher_PutGetAndOrder_UsesOneUse()
    {
        var code = NewCode();
        await PutVoucherAsync(code, 10, 5);
        var customer = await NewCustomerAsync();
        var sku = await NewStockAsync(2);

        var response = await PlaceOrderAsync(customer, sku, code);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await BodyAsync(response));
        var voucher = await GetVoucherAsync(code);
        Assert.AreEqual(10, voucher.GetProperty("percentOff").GetInt32(), "percentOff");
        Assert.AreEqual(5, voucher.GetProperty("maxUses").GetInt32(), "maxUses");
        Assert.AreEqual(4, voucher.GetProperty("usesLeft").GetInt32(), "usesLeft after one order");
    }

    [TestMethod]
    public async Task Voucher_UnknownCode_Returns400()
    {
        var customer = await NewCustomerAsync();
        var sku = await NewStockAsync(2);

        var response = await PlaceOrderAsync(customer, sku, NewCode());

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, await BodyAsync(response));
    }

    [TestMethod]
    public async Task Voucher_ParallelOrders_NeverMoreThanMaxUses()
    {
        const int maxUses = 5;
        const int orders = 30;
        var code = NewCode();
        await PutVoucherAsync(code, 10, maxUses);
        var customers = new List<Guid>();
        var skus = new List<string>();
        for (var i = 0; i < orders; i++)
        {
            customers.Add(await NewCustomerAsync());
            skus.Add(await NewStockAsync(1));
        }

        var responses = await Task.WhenAll(Enumerable.Range(0, orders).Select(i => PlaceOrderAsync(customers[i], skus[i], code, quantity: 1)));

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicts = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.AreEqual(maxUses, created, $"Orders placed with a voucher of {maxUses} uses (409: {conflicts}, other: {Describe(responses)}).");
        Assert.AreEqual(orders - maxUses, conflicts, $"Orders refused with 409 ({Describe(responses)}).");
        Assert.AreEqual(0, (await GetVoucherAsync(code)).GetProperty("usesLeft").GetInt32(), "usesLeft");
        var reserved = 0;
        foreach (var sku in skus)
        {
            reserved += (await s_client.GetFromJsonAsync<JsonElement>($"/inventory/{sku}", TestContext.CancellationToken)).GetProperty("reserved").GetInt32();
        }

        Assert.AreEqual(maxUses, reserved, "Units reserved: a refused order must reserve nothing.");
    }

    [TestMethod]
    public async Task Voucher_SameCustomerParallel_OneOrderOthersConflict()
    {
        const int orders = 10;
        var code = NewCode();
        await PutVoucherAsync(code, 10, 100);
        var customer = await NewCustomerAsync();
        var skus = new List<string>();
        for (var i = 0; i < orders; i++)
        {
            skus.Add(await NewStockAsync(1));
        }

        var responses = await Task.WhenAll(skus.Select(sku => PlaceOrderAsync(customer, sku, code, quantity: 1)));

        Assert.AreEqual(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created), $"One customer used the voucher more than once, or never ({Describe(responses)}).");
        Assert.AreEqual(orders - 1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict), $"The other orders must get 409 ({Describe(responses)}).");
        Assert.AreEqual(99, (await GetVoucherAsync(code)).GetProperty("usesLeft").GetInt32(), "usesLeft: a refused order must not use the voucher.");
    }

    [TestMethod]
    public async Task Voucher_UsedBefore_Returns409AndReservesNothing()
    {
        var code = NewCode();
        await PutVoucherAsync(code, 10, 100);
        var customer = await NewCustomerAsync();
        var first = await NewStockAsync(2);
        var second = await NewStockAsync(2);
        var placed = await PlaceOrderAsync(customer, first, code);
        Assert.AreEqual(HttpStatusCode.Created, placed.StatusCode, await BodyAsync(placed));

        var response = await PlaceOrderAsync(customer, second, code);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode, await BodyAsync(response));
        var stock = await s_client.GetFromJsonAsync<JsonElement>($"/inventory/{second}", TestContext.CancellationToken);
        Assert.AreEqual(0, stock.GetProperty("reserved").GetInt32(), "The refused order reserved stock.");
    }

    [TestMethod]
    public async Task PutVoucher_ParallelSameNewCode_NoServerError()
    {
        // A race on a new key does not show on each try: run many rounds and count each failed call.
        var failures = new List<string>();
        for (var round = 0; round < 60; round++)
        {
            var code = NewCode();
            var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
                s_client.PutAsJsonAsync($"/vouchers/{code}", new { percentOff = 15, maxUses = 50 }, TestContext.CancellationToken)));
            foreach (var failed in responses.Where(r => !r.IsSuccessStatusCode))
            {
                failures.Add(await BodyAsync(failed));
            }
        }

        Assert.IsEmpty(failures, $"{failures.Count} of 720 parallel PUT calls for new codes failed. First: {failures.FirstOrDefault()}");
    }

    [TestMethod]
    public async Task PutVoucher_Again_ChangesSettingsAndKeepsUses()
    {
        var code = NewCode();
        await PutVoucherAsync(code, 10, 5);
        var placed = await PlaceOrderAsync(await NewCustomerAsync(), await NewStockAsync(2), code);
        Assert.AreEqual(HttpStatusCode.Created, placed.StatusCode, await BodyAsync(placed));

        await PutVoucherAsync(code, 20, 8);

        var voucher = await GetVoucherAsync(code);
        Assert.AreEqual(20, voucher.GetProperty("percentOff").GetInt32(), "percentOff");
        Assert.AreEqual(7, voucher.GetProperty("usesLeft").GetInt32(), "usesLeft after the change of maxUses");
    }

    [TestMethod]
    public async Task VoucherReport_OrderAt2230Utc_CountsOnNextAmsterdamDay()
    {
        var code = NewCode();
        await PutVoucherAsync(code, 10, 10);
        var customer = await NewCustomerAsync();
        var sku = await NewStockAsync(2);
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 8, 1, 22, 30, 0, TimeSpan.Zero));
        try
        {
            var placed = await PlaceOrderAsync(customer, sku, code, quantity: 2, unitPrice: 25m);
            Assert.AreEqual(HttpStatusCode.Created, placed.StatusCode, await BodyAsync(placed));
        }
        finally
        {
            s_factory.Time.SetUtcNow(ProbeFactory.Start);
        }

        var nextDay = await VoucherRowsAsync("2026-08-02", code);
        var sameDay = await VoucherRowsAsync("2026-08-01", code);

        Assert.AreEqual(1, nextDay.Count, "22:30 UTC on 1 August is 00:30 on 2 August in Amsterdam: the order must be on 2 August.");
        Assert.AreEqual("2026-08-02", nextDay[0].GetProperty("day").GetString(), "day");
        Assert.AreEqual(1, nextDay[0].GetProperty("orders").GetInt32(), "orders");
        Assert.AreEqual(5m, nextDay[0].GetProperty("discount").GetDecimal(), "discount: 10% of 50.00");
        Assert.AreEqual(0, sameDay.Sum(row => row.GetProperty("orders").GetInt32()), "The order is not on 1 August in Amsterdam.");
    }

    [TestMethod]
    public async Task SalesReport_OrderAt2230Utc_CountsOnNextAmsterdamDay()
    {
        var customer = await NewCustomerAsync();
        var sku = await NewStockAsync(2);
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 7, 1, 22, 30, 0, TimeSpan.Zero));
        try
        {
            var placed = await PlaceOrderAsync(customer, sku, voucherCode: null, quantity: 2, unitPrice: 3.75m);
            Assert.AreEqual(HttpStatusCode.Created, placed.StatusCode, await BodyAsync(placed));
        }
        finally
        {
            s_factory.Time.SetUtcNow(ProbeFactory.Start);
        }

        var nextDay = await s_client.GetFromJsonAsync<JsonElement>("/reports/sales?from=2026-07-02&to=2026-07-02", TestContext.CancellationToken);
        var sameDay = await s_client.GetFromJsonAsync<JsonElement>("/reports/sales?from=2026-07-01&to=2026-07-01", TestContext.CancellationToken);

        Assert.AreEqual(1, nextDay.GetProperty("days")[0].GetProperty("orders").GetInt32(), "22:30 UTC on 1 July is 00:30 on 2 July in Amsterdam: the sales report must count the order on 2 July.");
        Assert.AreEqual(7.5m, nextDay.GetProperty("totalNetRevenue").GetDecimal(), "net revenue of 2 July");
        Assert.AreEqual(0, sameDay.GetProperty("days")[0].GetProperty("orders").GetInt32(), "The order is not on 1 July in Amsterdam.");
    }

    [TestMethod]
    public async Task Orders_SqlPlans_HaveNoImplicitConversionOfColumns()
    {
        var code = NewCode();
        await PutVoucherAsync(code, 10, 10);
        await PutVoucherAsync(code, 10, 20);
        var customer = await NewCustomerAsync();
        var sku = await NewStockAsync(2);
        await NewStockAsync(1, sku);
        var placed = await PlaceOrderAsync(customer, sku, code);
        Assert.AreEqual(HttpStatusCode.Created, placed.StatusCode, await BodyAsync(placed));
        var reserve = await s_client.PostAsJsonAsync($"/inventory/{sku}/reservations", new { quantity = 1 }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, reserve.StatusCode, await BodyAsync(reserve));
        await GetVoucherAsync(code);
        await VoucherRowsAsync("2026-10-16", code);
        (await s_client.GetAsync(new Uri("/reports/sales?from=2026-10-16&to=2026-10-16", UriKind.Relative), TestContext.CancellationToken)).EnsureSuccessStatusCode();

        var converted = new List<string>();
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(TestContext.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SUBSTRING(st.text, 1, 400), CAST(qp.query_plan AS nvarchar(max))
            FROM sys.dm_exec_query_stats AS qs
            CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st
            CROSS APPLY sys.dm_exec_query_plan(qs.plan_handle) AS qp
            WHERE qp.dbid = DB_ID()
              AND CAST(qp.query_plan AS nvarchar(max)) LIKE N'%CONVERT_IMPLICIT(nvarchar%'
            """;
        await using (var reader = await command.ExecuteReaderAsync(TestContext.CancellationToken))
        {
            while (await reader.ReadAsync(TestContext.CancellationToken))
            {
                // Only a conversion of a column of an application table counts, not of a parameter.
                var match = s_columnConversion.Match(reader.GetString(1));
                if (match.Success)
                {
                    converted.Add(match.Value + " in " + reader.GetString(0).ReplaceLineEndings(" "));
                }
            }
        }

        Assert.AreEqual(0, converted.Count, "SQL Server converts a varchar column to nvarchar (CONVERT_IMPLICIT, no index seek) in: " + string.Join(" || ", converted));
    }

    private static string NewCode() => "PRB" + Guid.CreateVersion7().ToString("N")[^10..].ToUpperInvariant();

    private async Task<Guid> NewCustomerAsync()
    {
        var response = await s_client.PostAsJsonAsync(
            "/customers",
            new { name = "Probe Customer", email = $"probe-{Guid.CreateVersion7():N}@example.com", kind = 0, companyName = (string?)null },
            TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await BodyAsync(response));
        return (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken)).GetProperty("id").GetGuid();
    }

    private async Task<string> NewStockAsync(int quantity, string? sku = null)
    {
        sku ??= "PRB-" + Guid.CreateVersion7().ToString("N")[^12..].ToUpperInvariant();
        var response = await s_client.PostAsJsonAsync($"/inventory/{sku}/receipts", new { quantity }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await BodyAsync(response));
        return sku;
    }

    private async Task PutVoucherAsync(string code, int percentOff, int maxUses)
    {
        var response = await s_client.PutAsJsonAsync($"/vouchers/{code}", new { percentOff, maxUses }, TestContext.CancellationToken);
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT /vouchers/{code}: {(int)response.StatusCode} {await BodyAsync(response)}");
    }

    private async Task<JsonElement> GetVoucherAsync(string code)
    {
        var response = await s_client.GetAsync(new Uri($"/vouchers/{code}", UriKind.Relative), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await BodyAsync(response));
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken);
    }

    private async Task<List<JsonElement>> VoucherRowsAsync(string day, string code)
    {
        var response = await s_client.GetAsync(new Uri($"/reports/vouchers?from={day}&to={day}", UriKind.Relative), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await BodyAsync(response));
        var rows = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.CancellationToken);
        return [.. rows.EnumerateArray().Where(row => string.Equals(row.GetProperty("code").GetString(), code, StringComparison.OrdinalIgnoreCase))];
    }

    private Task<HttpResponseMessage> PlaceOrderAsync(Guid customerId, string sku, string? voucherCode, int quantity = 2, decimal unitPrice = 25m) =>
        s_client.PostAsJsonAsync(
            "/orders",
            new { customerId, lines = new[] { new { sku, quantity, unitPrice } }, voucherCode },
            s_json,
            TestContext.CancellationToken);

    private async Task<string> BodyAsync(HttpResponseMessage response) =>
        $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.CancellationToken)}";

    private static string Describe(IEnumerable<HttpResponseMessage> responses) =>
        string.Join(", ", responses.GroupBy(r => (int)r.StatusCode).OrderBy(g => g.Key).Select(g => $"{g.Key} x {g.Count()}"));

    private sealed class ProbeFactory : WebApplicationFactory<Program>
    {
        public static readonly DateTimeOffset Start = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

        public ProbeClock Time { get; } = new(Start);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", ConnectionString);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(Time);
            });
        }
    }

    // A clock that can go back, so that each test can place an order at its own instant.
    private sealed class ProbeClock(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public void SetUtcNow(DateTimeOffset value) => _now = value;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
