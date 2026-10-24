using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-50 (discount code on a new order).
// E* tests check the examples of the item. H* tests check the rules with values that no example gives.
[TestClass]
[DoNotParallelize]
public sealed class OrderDiscountProbe
{
    public TestContext TestContext { get; set; } = null!;

    private static object Line(string sku, int quantity, decimal unitPrice) => new { sku, quantity, unitPrice };

    private static async Task<OrdersApiFactory> StartAsync(DateTimeOffset? now, CancellationToken ct)
    {
        var factory = new OrdersApiFactory();
        if (now is { } t)
        {
            factory.Time.SetUtcNow(t);
        }

        await factory.MigrateAsync(ct);
        return factory;
    }

    private static async Task<(HttpStatusCode Status, JsonDocument Body)> PlaceAsync(
        HttpClient client, Guid customer, object[] lines, string? code, CancellationToken ct)
    {
        object request = code is null
            ? new { customerId = customer, lines }
            : new { customerId = customer, lines, discountCode = code };
        var response = await client.PostAsJsonAsync("/orders", request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text));
    }

    private static decimal Amount(JsonElement order, string name)
    {
        foreach (var p in order.EnumerateObject())
        {
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value.GetDecimal();
            }
        }

        throw new AssertFailedException($"The order has no field {name}: {order}");
    }

    private static void AssertAmounts(JsonElement order, decimal subtotal, decimal discount, decimal total)
    {
        Assert.AreEqual(subtotal, Amount(order, "subtotal"), "subtotal");
        Assert.AreEqual(discount, Amount(order, "discount"), "discount");
        Assert.AreEqual(total, Amount(order, "total"), "total");
    }

    private static void AssertNamesDiscountCode(HttpStatusCode status, JsonDocument body)
    {
        Assert.AreEqual((HttpStatusCode)422, status, body.RootElement.ToString());
        Assert.IsTrue(body.RootElement.ToString().Contains("discountCode", StringComparison.OrdinalIgnoreCase),
            "The problem details body does not name discountCode: " + body.RootElement);
    }

    private static async Task<JsonElement[]> ListAsync(HttpClient client, Guid customer, CancellationToken ct)
    {
        var json = await client.GetStringAsync($"/orders?customerId={customer}", ct);
        return [.. JsonDocument.Parse(json).RootElement.EnumerateArray()];
    }

    [TestMethod]
    public async Task E1_Welcome10_DiscountOnSubtotal_RoundedHalfAwayFromZero()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(),
            [Line("SKU-1", 2, 24.99m), Line("SKU-2", 1, 0.07m)], "WELCOME10", ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        AssertAmounts(body.RootElement, 50.05m, 5.01m, 45.04m);
    }

    [TestMethod]
    public async Task E2_Code_IgnoresCaseAndSpaces()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 1, 80.00m)], " welcome10 ", ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        AssertAmounts(body.RootElement, 80.00m, 8.00m, 72.00m);
    }

    [TestMethod]
    public async Task E3_ExpiredAndUnknownCodes_Return422_AndStoreNothing()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var client = factory.CreateClient();
        var customer = Guid.CreateVersion7();
        foreach (var code in new[] { "SPRING25", "NOPE" })
        {
            var (status, body) = await PlaceAsync(client, customer, [Line("SKU-1", 1, 80.00m)], code, ct);
            AssertNamesDiscountCode(status, body);
        }

        Assert.AreEqual(0, (await ListAsync(client, customer, ct)).Length, "A refused order was stored.");
    }

    [TestMethod]
    public async Task E4_Minimum_Exactly50Qualifies_49_99DoesNot()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var client = factory.CreateClient();
        var (ok, okBody) = await PlaceAsync(client, Guid.CreateVersion7(), [Line("SKU-1", 1, 50.00m)], "WELCOME10", ct);
        Assert.AreEqual(HttpStatusCode.Created, ok, okBody.RootElement.ToString());
        AssertAmounts(okBody.RootElement, 50.00m, 5.00m, 45.00m);
        var (low, lowBody) = await PlaceAsync(client, Guid.CreateVersion7(), [Line("SKU-1", 1, 49.99m)], "WELCOME10", ct);
        AssertNamesDiscountCode(low, lowBody);
    }

    [TestMethod]
    public async Task E5_NoCode_DiscountZero()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 3, 10.00m)], null, ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        AssertAmounts(body.RootElement, 30.00m, 0.00m, 30.00m);
    }

    [TestMethod]
    public async Task E6_List_ShowsTheSameAmounts()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var client = factory.CreateClient();
        var customer = Guid.CreateVersion7();
        var (status, body) = await PlaceAsync(client, customer, [Line("SKU-1", 2, 24.99m), Line("SKU-2", 1, 0.07m)], "WELCOME10", ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        var list = await ListAsync(client, customer, ct);
        Assert.AreEqual(1, list.Length);
        AssertAmounts(list[0], 50.05m, 5.01m, 45.04m);
    }

    [TestMethod]
    public async Task H1_Rounding_HalfAwayFromZero_NotToEven()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 1, 50.25m)], "WELCOME10", ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        AssertAmounts(body.RootElement, 50.25m, 5.03m, 45.22m);
    }

    [TestMethod]
    public async Task H2_Welcome10_ValidToTheEndOfItsLastDayUtc()
    {
        var ct = TestContext.CancellationToken;
        await using (var factory = await StartAsync(new DateTimeOffset(2026, 12, 31, 23, 59, 59, TimeSpan.Zero), ct))
        {
            var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 1, 80.00m)], "WELCOME10", ct);
            Assert.AreEqual(HttpStatusCode.Created, status, "Last second of the end day: " + body.RootElement);
        }

        await using (var factory = await StartAsync(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero), ct))
        {
            var (status, body) = await PlaceAsync(factory.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 1, 80.00m)], "WELCOME10", ct);
            AssertNamesDiscountCode(status, body);
        }
    }

    [TestMethod]
    public async Task H3_Spring25_ValidOnItsLastDay()
    {
        var ct = TestContext.CancellationToken;
        await using var factory = await StartAsync(null, ct);
        // The fake clock of the factory cannot go back, so this test replaces it with a clock in May 2026.
        await using var inMay = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<TimeProvider>();
            s.AddSingleton<TimeProvider>(new FakeTimeProvider(new DateTimeOffset(2026, 5, 31, 22, 0, 0, TimeSpan.Zero)));
        }));
        var (status, body) = await PlaceAsync(inMay.CreateClient(), Guid.CreateVersion7(), [Line("SKU-1", 1, 80.00m)], "SPRING25", ct);
        Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        AssertAmounts(body.RootElement, 80.00m, 20.00m, 60.00m);
    }

    [TestMethod]
    public async Task H4_Discount_IsStored_AndSurvivesARestart()
    {
        var ct = TestContext.CancellationToken;
        var customer = Guid.CreateVersion7();
        await using (var factory = await StartAsync(null, ct))
        {
            var (status, body) = await PlaceAsync(factory.CreateClient(), customer, [Line("SKU-1", 1, 120.00m)], "WELCOME10", ct);
            Assert.AreEqual(HttpStatusCode.Created, status, body.RootElement.ToString());
        }

        await using (var factory = await StartAsync(null, ct))
        {
            var list = await ListAsync(factory.CreateClient(), customer, ct);
            Assert.AreEqual(1, list.Length);
            AssertAmounts(list[0], 120.00m, 12.00m, 108.00m);
        }
    }
}
