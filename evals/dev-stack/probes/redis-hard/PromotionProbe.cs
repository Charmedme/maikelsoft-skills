using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for redis-hard. Two app instances (nodes) share one PostgreSQL database and one
// Valkey server, as the replicas in deploy/compose.yml do. The probe uses HTTP only, except one
// test that writes a cache entry the way the current version (before the change) writes it.
[TestClass]
[DoNotParallelize]
public sealed class PromotionProbe
{
    private static IContainer? s_valkey;
    private static string s_cache = string.Empty;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task StartValkeyAsync(TestContext context)
    {
        s_valkey = new ContainerBuilder("valkey/valkey:9.1")
            .WithPortBinding(6379, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Ready to accept connections"))
            .Build();
        await s_valkey.StartAsync(context.CancellationToken);
        s_cache = $"{s_valkey.Hostname}:{s_valkey.GetMappedPublicPort(6379)}";

        // Also for code that reads the connection string before the host applies UseSetting.
        Environment.SetEnvironmentVariable("ConnectionStrings__Cache", s_cache);
    }

    [ClassCleanup]
    public static async Task StopValkeyAsync()
    {
        if (s_valkey is not null)
        {
            await s_valkey.DisposeAsync();
        }
    }

    // The feature itself: the promotion shows on the product page and checkout charges it.
    [TestMethod]
    public async Task Promotion_IsShownAndCharged()
    {
        var ct = TestContext.CancellationToken;
        await using var node = new Node();
        await MigrateAsync(node, ct);
        using var client = node.CreateClient();

        var sku = await CreateProductAsync(client, 10.00m, ct);
        await StartPromotionAsync(client, sku, 20, DateTimeOffset.UtcNow.AddHours(1), ct);

        using var product = await GetProductAsync(client, sku, ct);
        Assert.AreEqual(8.00m, Property(product.RootElement, "price").GetDecimal(), "price during the promotion");
        Assert.AreEqual(10.00m, Property(product.RootElement, "listPrice").GetDecimal(), "listPrice during the promotion");
        var promotion = Property(product.RootElement, "promotion");
        Assert.AreEqual(JsonValueKind.Object, promotion.ValueKind, "promotion during the promotion");
        Assert.AreEqual(20m, Property(promotion, "percentOff").GetDecimal(), "promotion.percentOff");

        Assert.AreEqual(8.00m, await OrderUnitPriceAsync(client, sku, ct), "unit price of an order during the promotion");
    }

    // Trap: HybridCache has no backplane, and the fixture keeps L1 for 30 minutes.
    [TestMethod]
    public async Task EndPromotionOnNodeA_NodeBShowsListPrice()
    {
        var ct = TestContext.CancellationToken;
        await using var nodeA = new Node();
        await using var nodeB = new Node();
        await MigrateAsync(nodeA, ct);
        using var a = nodeA.CreateClient();
        using var b = nodeB.CreateClient();

        var sku = await CreateProductAsync(a, 10.00m, ct);
        await StartPromotionAsync(a, sku, 20, DateTimeOffset.UtcNow.AddHours(1), ct);
        Assert.AreEqual(8.00m, await PriceAsync(b, sku, ct), "first read on node B");
        Assert.AreEqual(8.00m, await PriceAsync(a, sku, ct), "first read on node A");

        await EndPromotionAsync(a, sku, ct);

        Assert.AreEqual(10.00m, await PriceAsync(b, sku, ct), "product page on node B after the promotion ended on node A");
        Assert.AreEqual(10.00m, await PriceAsync(a, sku, ct), "product page on node A after the promotion ended on node A");
    }

    // Trap (existing code): checkout reads the price through the product cache.
    [TestMethod]
    public async Task EndPromotionOnNodeA_CheckoutOnNodeBChargesListPrice()
    {
        var ct = TestContext.CancellationToken;
        await using var nodeA = new Node();
        await using var nodeB = new Node();
        await MigrateAsync(nodeA, ct);
        using var a = nodeA.CreateClient();
        using var b = nodeB.CreateClient();

        var sku = await CreateProductAsync(a, 10.00m, ct);
        await StartPromotionAsync(a, sku, 20, DateTimeOffset.UtcNow.AddHours(1), ct);
        Assert.AreEqual(8.00m, await OrderUnitPriceAsync(b, sku, ct), "order on node B during the promotion");
        Assert.AreEqual(8.00m, await PriceAsync(b, sku, ct), "product page on node B during the promotion");

        await EndPromotionAsync(a, sku, ct);

        Assert.AreEqual(10.00m, await OrderUnitPriceAsync(b, sku, ct), "order on node B after the promotion ended on node A");
    }

    // Trap: a cached price outlives endsAt (the fixture caches for 30 minutes).
    [TestMethod]
    public async Task PromotionEndsAt_PriceReturnsToListPrice()
    {
        var ct = TestContext.CancellationToken;
        await using var node = new Node();
        await MigrateAsync(node, ct);
        using var client = node.CreateClient();

        var sku = await CreateProductAsync(client, 10.00m, ct);
        var endsAt = DateTimeOffset.UtcNow.AddSeconds(4);
        await StartPromotionAsync(client, sku, 20, endsAt, ct);
        Assert.AreEqual(8.00m, await PriceAsync(client, sku, ct), "first read during the promotion");
        Assert.AreEqual(8.00m, await PriceAsync(client, sku, ct), "second read during the promotion");

        var wait = endsAt - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1.5);
        if (wait > TimeSpan.Zero)
        {
            await Task.Delay(wait, ct);
        }

        using var product = await GetProductAsync(client, sku, ct);
        Assert.AreEqual(10.00m, Property(product.RootElement, "price").GetDecimal(), "price after endsAt");
        Assert.AreEqual(JsonValueKind.Null, Property(product.RootElement, "promotion").ValueKind, "promotion after endsAt");
        Assert.AreEqual(10.00m, await OrderUnitPriceAsync(client, sku, ct), "unit price of an order after endsAt");
    }

    // Trap (existing code): the key has no version segment. During a rolling deploy the old version
    // keeps writing ProductResponse without listPrice and promotion under the same key.
    [TestMethod]
    public async Task CacheEntryOfPreviousVersion_IsNotServed()
    {
        var ct = TestContext.CancellationToken;
        await using var nodeA = new Node();
        await MigrateAsync(nodeA, ct);
        using var a = nodeA.CreateClient();
        var sku = await CreateProductAsync(a, 10.00m, ct);
        Guid id;
        using (var created = await GetProductAsync(a, sku, ct))
        {
            id = Property(created.RootElement, "id").GetGuid();
        }

        await WriteAsPreviousVersionAsync(new PreviousProductResponse(id, sku, "Probe product", 10.00m, 12.10m, "Standard", true), ct);

        await using var nodeB = new Node();
        using var b = nodeB.CreateClient();
        using var product = await GetProductAsync(b, sku, ct);
        Assert.AreEqual(10.00m, Property(product.RootElement, "listPrice").GetDecimal(), "listPrice on a fresh node next to an entry of the previous version");
        Assert.AreEqual(10.00m, Property(product.RootElement, "price").GetDecimal(), "price on a fresh node next to an entry of the previous version");
    }

    // The cache setup and the key of the fixture (before the change): InstanceName "orders:", key "product:{SKU}".
    private static async Task WriteAsPreviousVersionAsync(PreviousProductResponse value, CancellationToken ct)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = s_cache;
            options.InstanceName = "orders:";
        });
        services.AddHybridCache();
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        await cache.SetAsync($"product:{value.Sku}", value, new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(30) }, ["products"], ct);
    }

    private static async Task MigrateAsync(Node node, CancellationToken ct)
    {
        await using var scope = node.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task<string> CreateProductAsync(HttpClient client, decimal price, CancellationToken ct)
    {
        var sku = "PRB-" + Guid.CreateVersion7().ToString("N")[12..].ToUpperInvariant();
        using var response = await client.PostAsJsonAsync("/products", new { sku, name = "Probe product", price, vatRate = 0 }, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /products returned {(int)response.StatusCode}");
        return sku;
    }

    private static async Task StartPromotionAsync(HttpClient client, string sku, int percentOff, DateTimeOffset endsAt, CancellationToken ct)
    {
        using var response = await client.PutAsJsonAsync($"/products/{sku}/promotion", new { percentOff, endsAt }, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"PUT /products/{sku}/promotion returned {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}");
    }

    private static async Task EndPromotionAsync(HttpClient client, string sku, CancellationToken ct)
    {
        using var response = await client.DeleteAsync(new Uri($"/products/{sku}/promotion", UriKind.Relative), ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"DELETE /products/{sku}/promotion returned {(int)response.StatusCode}");
    }

    private static async Task<JsonDocument> GetProductAsync(HttpClient client, string sku, CancellationToken ct)
    {
        using var response = await client.GetAsync(new Uri($"/products/{sku}", UriKind.Relative), ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET /products/{sku} returned {(int)response.StatusCode}");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<decimal> PriceAsync(HttpClient client, string sku, CancellationToken ct)
    {
        using var document = await GetProductAsync(client, sku, ct);
        return Property(document.RootElement, "price").GetDecimal();
    }

    private static async Task<decimal> OrderUnitPriceAsync(HttpClient client, string sku, CancellationToken ct)
    {
        using var customerResponse = await client.PostAsJsonAsync(
            "/customers",
            new { name = "Probe Customer", email = $"probe-{Guid.CreateVersion7():N}@example.com", kind = 0 },
            ct);
        Assert.IsTrue(customerResponse.IsSuccessStatusCode, $"POST /customers returned {(int)customerResponse.StatusCode}");
        using var customer = JsonDocument.Parse(await customerResponse.Content.ReadAsStringAsync(ct));
        var customerId = Property(customer.RootElement, "id").GetGuid();

        using var orderResponse = await client.PostAsJsonAsync("/orders", new { customerId, lines = new[] { new { sku, quantity = 1 } } }, ct);
        var body = await orderResponse.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(orderResponse.IsSuccessStatusCode, $"POST /orders returned {(int)orderResponse.StatusCode}: {body}");
        using var order = JsonDocument.Parse(body);
        var line = Property(order.RootElement, "lines").EnumerateArray().Single();
        return Property(line, "unitPrice").GetDecimal();
    }

    private static JsonElement Property(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new AssertFailedException(string.Create(CultureInfo.InvariantCulture, $"The response has no property '{name}': {element}"));
    }

    // The shape of ProductResponse in the fixture, before the change.
    private sealed record PreviousProductResponse(Guid Id, string Sku, string Name, decimal Price, decimal GrossPrice, string VatRate, bool Active);

    private sealed class Node : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:Cache", s_cache);
        }
    }
}
