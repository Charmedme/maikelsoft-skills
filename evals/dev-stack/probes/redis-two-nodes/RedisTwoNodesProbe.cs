using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for redis-trap. Two app instances (nodes) share one PostgreSQL database and one
// Valkey server, as three instances behind a load balancer do. The probe uses only HTTP:
// POST /orders, GET /orders/{id} and POST /orders/{id}/cancel.
[TestClass]
[DoNotParallelize]
public sealed class RedisTwoNodesProbe
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

    // Trap: HybridCache has no backplane. After a remove on node A, node B serves its L1 copy.
    [TestMethod]
    public async Task CancelOnNodeA_NodeBShowsCancelled()
    {
        var ct = TestContext.CancellationToken;
        await using var nodeA = new Node();
        await using var nodeB = new Node();
        await MigrateAsync(nodeA, ct);
        using var a = nodeA.CreateClient();
        using var b = nodeB.CreateClient();

        var id = await CreateOrderAsync(a, ct);
        Assert.AreEqual("placed", await StatusAsync(b, id, ct), "first read on node B");
        Assert.AreEqual("placed", await StatusAsync(a, id, ct), "first read on node A");

        using var cancel = await a.PostAsync(new Uri($"/orders/{id}/cancel", UriKind.Relative), JsonContent.Create(new { }), ct);
        Assert.IsTrue(cancel.IsSuccessStatusCode, $"cancel on node A returned {(int)cancel.StatusCode}");

        Assert.AreEqual("cancelled", await StatusAsync(b, id, ct), "node B after the cancel on node A");
        Assert.AreEqual("cancelled", await StatusAsync(a, id, ct), "node A after its own cancel");
    }

    // Trap: the EF entity Order has private setters. A JSON round trip gives an empty Id and no lines.
    [TestMethod]
    public async Task GetTwice_SameNode_ReturnsLinesAndId()
    {
        var ct = TestContext.CancellationToken;
        await using var nodeA = new Node();
        await using var nodeB = new Node();
        await MigrateAsync(nodeA, ct);
        using var a = nodeA.CreateClient();
        using var b = nodeB.CreateClient();

        var id = await CreateOrderAsync(a, ct);
        foreach (var (client, read) in new[] { (a, "first read on node A"), (a, "second read on node A"), (b, "read on node B") })
        {
            using var document = await GetOrderAsync(client, id, ct);
            var root = document.RootElement;
            Assert.AreEqual(id, Property(root, "id").GetGuid(), $"id, {read}");
            Assert.AreEqual(2, Property(root, "lines").GetArrayLength(), $"lines, {read}");
        }
    }

    private static async Task MigrateAsync(Node node, CancellationToken ct)
    {
        await using var scope = node.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
    }

    private static async Task<Guid> CreateOrderAsync(HttpClient client, CancellationToken ct)
    {
        var body = new
        {
            customerId = Guid.CreateVersion7(),
            lines = new[]
            {
                new { sku = "SKU-1", quantity = 1, unitPrice = 2.00m },
                new { sku = "SKU-2", quantity = 2, unitPrice = 3.00m },
            },
        };
        using var response = await client.PostAsJsonAsync("/orders", body, ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return Property(document.RootElement, "id").GetGuid();
    }

    private static async Task<JsonDocument> GetOrderAsync(HttpClient client, Guid id, CancellationToken ct)
    {
        using var response = await client.GetAsync(new Uri($"/orders/{id}", UriKind.Relative), ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"GET /orders/{id} returned {(int)response.StatusCode}");
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }

    private static async Task<string> StatusAsync(HttpClient client, Guid id, CancellationToken ct)
    {
        using var document = await GetOrderAsync(client, id, ct);
        var status = Property(document.RootElement, "status");
        return status.ValueKind == JsonValueKind.Number
            ? status.GetInt32() switch { 0 => "placed", 1 => "cancelled", 2 => "shipped", var n => n.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            : (status.GetString() ?? string.Empty).ToLowerInvariant();
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

        throw new AssertFailedException($"The response has no property '{name}': {element}");
    }

    private sealed class Node : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            builder.UseSetting("ConnectionStrings:Cache", s_cache);
        }
    }
}
