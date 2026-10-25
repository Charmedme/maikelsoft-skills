using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for elasticsearch-hard. Each test gets an empty database and an Elasticsearch node without
// product indexes (the node of AssemblyFixture), starts the app, and uses only its HTTP API. A few tests
// also change the Elasticsearch node directly, as production can: an index of the previous release,
// or the write block that Elasticsearch sets when the disk is full.
[TestClass]
[DoNotParallelize]
public sealed class ProductSearchProbe
{
    private static readonly TimeSpan s_wait = TimeSpan.FromSeconds(40);

    // The index that the previous release (the fixture) creates on its first start, with its mapping.
    private const string PreviousReleaseIndex = """
        {"mappings":{"dynamic":"strict","properties":{
          "id":{"type":"keyword"},"sku":{"type":"keyword"},"name":{"type":"text"},"brand":{"type":"text"},
          "category":{"type":"keyword"},"price":{"type":"scaled_float","scaling_factor":100.0},
          "active":{"type":"boolean"},"version":{"type":"long"}}}}
        """;

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Search_FindsNewProductByName()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Probe Zeppelinlamp", "Lumina", 3, ct);

        var found = await SearchUntilAsync(client, "q=zeppelinlamp", body => body.Contains(sku, StringComparison.Ordinal), ct);

        Assert.IsTrue(found.Ok, $"A new product with stock was not found by its name within {s_wait.TotalSeconds} s: {found.Last}");
    }

    // Trap: a text field without a language analyzer does not match other word forms ("fiets" and "fietsen").
    [TestMethod]
    public async Task Search_DutchAndGermanDescriptions_MatchOtherWordForms()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Model K-77", "Lumina", 3, ct);
        await DescribeAsync(client, sku, "Stevige fietsen voor de stad", "Robuste Fahrräder für die Stadt", ct);

        var dutch = await SearchUntilAsync(client, "q=fiets&lang=nl", body => body.Contains(sku, StringComparison.Ordinal), ct);
        var german = await SearchUntilAsync(client, "q=Fahrrad&lang=de", body => body.Contains(sku, StringComparison.Ordinal), ct, TimeSpan.FromSeconds(10));

        Assert.IsTrue(dutch.Ok, $"'fiets' with lang=nl did not find the product with the Dutch description 'Stevige fietsen voor de stad': {dutch.Last}");
        Assert.IsTrue(german.Ok, $"'Fahrrad' with lang=de did not find the product with the German description 'Robuste Fahrräder für die Stadt': {german.Last}");
    }

    // Trap (existing code): the brand filter is a term query on a text field, so 'Gazelle' never matches.
    [TestMethod]
    public async Task Search_BrandFilter_FindsProductsOfThatBrand()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Probe Brandtest Omafiets", "Gazelle", 2, ct);

        var indexed = await SearchUntilAsync(client, "q=brandtest", body => body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(indexed.Ok, $"The product was not found by its name, so the brand filter cannot be tested: {indexed.Last}");
        var filtered = await SearchUntilAsync(client, "q=brandtest&brand=Gazelle", body => body.Contains(sku, StringComparison.Ordinal), ct, TimeSpan.FromSeconds(5));

        Assert.IsTrue(filtered.Ok, $"The brand filter 'Gazelle' does not find the product of brand Gazelle: {filtered.Last}");
    }

    // Trap: stock lives in another aggregate. A stock change that is indexed with the version of the product
    // gets a 409 (same version) and is dropped, so the product stays in the results.
    [TestMethod]
    public async Task Search_ProductWithoutAvailableStock_IsNotShown()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Probe Stocktest Kinderzitje", "Lumina", 1, ct);

        var shown = await SearchUntilAsync(client, "q=stocktest", body => body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(shown.Ok, $"The product with 1 item in stock was not found: {shown.Last}");

        using (var reserve = await client.PostAsJsonAsync($"/inventory/{sku}/reservations", new { quantity = 1 }, ct))
        {
            Assert.AreEqual(HttpStatusCode.OK, reserve.StatusCode, "The reservation of the last item failed.");
        }

        var gone = await SearchUntilAsync(client, "q=stocktest", body => !body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(gone.Ok, $"The product is still in the results {s_wait.TotalSeconds} s after its last item was reserved: {gone.Last}");
    }

    // Trap (existing code): from + size above 10,000 fails in Elasticsearch, and the unchecked response
    // looks like an empty page.
    [TestMethod]
    public async Task Search_PageBeyondResultWindow_ReturnsValidationProblem()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Probe Pagetest", "Lumina", 1, ct);
        var shown = await SearchUntilAsync(client, "q=pagetest", body => body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(shown.Ok, $"The product was not found: {shown.Last}");

        using var page2 = await client.GetAsync("/products/search?q=pagetest&page=2", ct);
        using var page501 = await client.GetAsync("/products/search?q=pagetest&page=501", ct);

        Assert.AreEqual(HttpStatusCode.OK, page2.StatusCode, "Page 2 must be a normal (empty) page.");
        Assert.AreEqual(HttpStatusCode.BadRequest, page501.StatusCode, $"Page 501 (from + size = 10,020) must be a validation problem: {await page501.Content.ReadAsStringAsync(ct)}");
    }

    // Trap (existing code): the worker checks only the HTTP status of the bulk call. A rejected item (429 while
    // the index has the disk-full write block) is deleted from the outbox and never indexed.
    [TestMethod]
    public async Task IndexWriteBlocked_ChangeArrivesAfterBlockIsLifted()
    {
        var ct = TestContext.CancellationToken;
        await using var app = await StartAsync(ct);
        using var client = app.CreateClient();
        using var search = SearchClient();
        var sku = await AddProductAsync(client, "Probe Blocktest", "Lumina", 2, ct);
        var shown = await SearchUntilAsync(client, "q=blocktest", body => body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(shown.Ok, $"The product was not found: {shown.Last}");

        await SetWriteBlockAsync(search, true, ct);
        try
        {
            await DescribeAsync(client, sku, "Lamp in de vorm van een kolibrie", null, ct);
            await Task.Delay(TimeSpan.FromSeconds(5), ct); // the worker tries while the block is on
        }
        finally
        {
            await SetWriteBlockAsync(search, false, ct);
        }

        var arrived = await SearchUntilAsync(client, "q=kolibrie&lang=nl", body => body.Contains(sku, StringComparison.Ordinal), ct);
        Assert.IsTrue(arrived.Ok, $"The description that was saved while the index was blocked never reached the search: {arrived.Last}");
    }

    // Trap (existing code): the index is created only when it does not exist, so the new mapping never
    // reaches the index of the previous release. With dynamic: strict, documents with the new fields fail.
    [TestMethod]
    public async Task LiveIndexFromThePreviousRelease_IsUpgraded()
    {
        var ct = TestContext.CancellationToken;
        using (var search = SearchClient())
        {
            await CleanSearchAsync(search, ct);
            using var create = await search.PutAsync("/products", new StringContent(PreviousReleaseIndex, Encoding.UTF8, "application/json"), ct);
            Assert.IsTrue(create.IsSuccessStatusCode, "The probe could not create the index of the previous release: " + await create.Content.ReadAsStringAsync(ct));
        }

        await using var app = await StartAsync(ct, cleanSearch: false);
        using var client = app.CreateClient();
        var sku = await AddProductAsync(client, "Probe Upgradetest", "Gazelle", 2, ct);
        await DescribeAsync(client, sku, "Lichte fietsen voor op vakantie", null, ct);

        var found = await SearchUntilAsync(client, "q=fiets&lang=nl&brand=Gazelle", body => body.Contains(sku, StringComparison.Ordinal), ct, TimeSpan.FromSeconds(60));

        Assert.IsTrue(found.Ok, $"With the index of the previous release in place, a new product with a Dutch description and brand Gazelle is not found: {found.Last}");
    }

    private static async Task<Node> StartAsync(CancellationToken ct, bool cleanSearch = true)
    {
        if (cleanSearch)
        {
            using var search = SearchClient();
            await CleanSearchAsync(search, ct);
        }

        var database = new NpgsqlConnectionStringBuilder(AssemblyFixture.ConnectionString)
        {
            Database = "probe_" + Guid.NewGuid().ToString("N")[..12],
        }.ConnectionString;
        var node = new Node(database);
        await using var scope = node.Services.CreateAsyncScope(); // starts the host, with its background services
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        return node;
    }

    private static HttpClient SearchClient()
    {
        var http = new HttpClient { BaseAddress = AssemblyFixture.SearchUri };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", AssemblyFixture.SearchApiKey);
        return http;
    }

    // Deletes every index that is not a system index. The probe tests do not run in parallel.
    private static async Task CleanSearchAsync(HttpClient search, CancellationToken ct)
    {
        using var list = await search.GetAsync("/_cat/indices?format=json&expand_wildcards=open,closed", ct);
        list.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync(ct));
        foreach (var index in json.RootElement.EnumerateArray().Select(e => e.GetProperty("index").GetString()!).Where(n => !n.StartsWith('.')))
        {
            using var delete = await search.DeleteAsync("/" + index, ct);
            delete.EnsureSuccessStatusCode();
        }
    }

    private static async Task SetWriteBlockAsync(HttpClient search, bool on, CancellationToken ct)
    {
        var body = on ? """{"index.blocks.read_only_allow_delete":true}""" : """{"index.blocks.read_only_allow_delete":null}""";
        using var list = await search.GetAsync("/_cat/indices?format=json", ct);
        using var json = JsonDocument.Parse(await list.Content.ReadAsStringAsync(ct));
        foreach (var index in json.RootElement.EnumerateArray().Select(e => e.GetProperty("index").GetString()!).Where(n => !n.StartsWith('.')))
        {
            using var response = await search.PutAsync($"/{index}/_settings", new StringContent(body, Encoding.UTF8, "application/json"), ct);
            Assert.IsTrue(response.IsSuccessStatusCode, $"The probe could not change the write block of {index}: {await response.Content.ReadAsStringAsync(ct)}");
        }
    }

    // Receives the stock first and then creates the product, so that the first document of the product
    // already has its stock. Then no test depends on the order in which the worker reads the outbox.
    private static async Task<string> AddProductAsync(HttpClient client, string name, string brand, int stock, CancellationToken ct)
    {
        var sku = "PRB-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        await ReceiveAsync(client, sku, stock, ct);
        using var response = await client.PostAsJsonAsync(
            "/products",
            new { sku, name, price = 49.95m, vatRate = 0, brand, category = "bikes" },
            ct);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, "POST /products failed: " + await response.Content.ReadAsStringAsync(ct));
        return sku;
    }

    private static async Task DescribeAsync(HttpClient client, string sku, string? nl, string? de, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync($"/products/{sku}/descriptions", new { nl, de }, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT /products/{sku}/descriptions returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
    }

    private static async Task ReceiveAsync(HttpClient client, string sku, int quantity, CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/inventory/{sku}/receipts", new { quantity }, ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "The stock receipt failed: " + await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<(bool Ok, string Last)> SearchUntilAsync(
        HttpClient client,
        string query,
        Func<string, bool> done,
        CancellationToken ct,
        TimeSpan? wait = null)
    {
        var end = DateTime.UtcNow + (wait ?? s_wait);
        var last = string.Empty;
        while (true)
        {
            using var response = await client.GetAsync("/products/search?" + query, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            last = $"{(int)response.StatusCode} {body[..Math.Min(body.Length, 300)]}";
            if (response.IsSuccessStatusCode && done(body))
            {
                return (true, last);
            }

            if (DateTime.UtcNow > end)
            {
                return (false, last);
            }

            await Task.Delay(250, ct);
        }
    }

    private sealed class Node(string database) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", database);
            builder.UseSetting("Smtp:Host", "localhost");
            builder.UseSetting("Search:Uri", AssemblyFixture.SearchUri.ToString());
            builder.UseSetting("Search:ApiKey", AssemblyFixture.SearchApiKey);
        }
    }
}
