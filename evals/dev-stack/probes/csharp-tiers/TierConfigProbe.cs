using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for csharp-trap. It checks the tier map from configuration through HTTP only.
[TestClass]
[DoNotParallelize]
public sealed class TierConfigProbe
{
    private static readonly Guid s_gold = Guid.Parse("0199f0a0-1111-7000-8000-000000000001");

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task GoldCustomerInConfiguration_GetsGoldTier()
    {
        await using var factory = Factory(($"Pricing:CustomerTiers:{s_gold}", "Gold"));

        var order = await PlaceOrderAsync(factory, s_gold, TestContext.CancellationToken);

        Assert.AreEqual("gold", Prop(order, "tier").ToString().ToLowerInvariant());
        Assert.AreEqual(10.00m, Prop(order, "discount").GetDecimal());
    }

    [TestMethod]
    public async Task CustomerNotInConfiguration_IsBronze()
    {
        await using var factory = Factory(($"Pricing:CustomerTiers:{s_gold}", "Gold"));

        var order = await PlaceOrderAsync(factory, Guid.CreateVersion7(), TestContext.CancellationToken);

        Assert.AreEqual("bronze", Prop(order, "tier").ToString().ToLowerInvariant());
        Assert.AreEqual(0m, Prop(order, "discount").GetDecimal());
    }

    [TestMethod]
    public async Task UnknownTierName_StopsTheApp()
    {
        await using var factory = Factory(($"Pricing:CustomerTiers:{s_gold}", "Gld"));

        Assert.Throws<Exception>(() => factory.CreateClient());
    }

    [TestMethod]
    public async Task CustomerIdThatIsNotAGuid_StopsTheApp()
    {
        await using var factory = Factory(("Pricing:CustomerTiers:customer-42", "Gold"));

        Assert.Throws<Exception>(() => factory.CreateClient());
    }

    private static WebApplicationFactory<Program> Factory(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Orders", AssemblyFixture.ConnectionString);
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });

    private static async Task<JsonElement> PlaceOrderAsync(WebApplicationFactory<Program> factory, Guid customerId, CancellationToken ct)
    {
        using var client = factory.CreateClient();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync(ct);
        }

        var body = new { customerId, lines = new[] { new { sku = "SKU-1", quantity = 1, unitPrice = 100.00m } } };
        var response = await client.PostAsJsonAsync("/orders", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.IsTrue(response.IsSuccessStatusCode, $"POST /orders returned {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static JsonElement Prop(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        Assert.Fail($"The order response has no '{name}' field: {element}");
        return default;
    }
}
