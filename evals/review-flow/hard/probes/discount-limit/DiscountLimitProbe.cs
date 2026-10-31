using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Hidden tests of SHOP-61. E tests check the criteria. H tests check docs/domain.md and traps of the existing code.
[TestClass]
[DoNotParallelize]
public sealed class DiscountLimitProbe
{

    public TestContext TestContext { get; set; } = null!;

    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    [ClassInitialize]
    public static async Task InitAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task CleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    private CancellationToken Ct => TestContext.CancellationToken;

    private static string Sku() => "P" + Guid.NewGuid().ToString("N")[..12];

    private static object Line(string sku, int quantity, decimal unitPrice) => new { sku, quantity, unitPrice };

    private static decimal Num(JsonElement item, string name)
    {
        foreach (var p in item.EnumerateObject())
        {
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value.GetDecimal();
            }
        }

        throw new AssertFailedException($"The field {name} is missing: {item}");
    }

    private static string Text(JsonElement item, string name)
    {
        foreach (var p in item.EnumerateObject())
        {
            if (p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return p.Value.ToString();
            }
        }

        throw new AssertFailedException($"The field {name} is missing: {item}");
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync(Ct);
        return (response.StatusCode, JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text).RootElement.Clone());
    }

    private async Task PutStockAsync(string sku, int available) =>
        (await s_client.PutAsJsonAsync("/stock", new { sku, available }, Ct)).EnsureSuccessStatusCode();

    private async Task<int> StockAsync(string sku)
    {
        var (_, body) = await SendAsync(await s_client.GetAsync(new Uri($"/stock/{sku}", UriKind.Relative), Ct));
        return (int)Num(body, "available");
    }

    private async Task<int> EventsAsync(Guid order)
    {
        var (_, body) = await SendAsync(await s_client.GetAsync(new Uri($"/orders/{order}/events", UriKind.Relative), Ct));
        return body.GetArrayLength();
    }

    private async Task<JsonElement> ReadAsync(Guid order)
    {
        var (status, body) = await SendAsync(await s_client.GetAsync(new Uri($"/orders/{order}", UriKind.Relative), Ct));
        Assert.AreEqual(HttpStatusCode.OK, status);
        return body;
    }

    private async Task<Guid> PlaceAsync(params object[] lines)
    {
        var (status, body) = await SendAsync(await s_client.PostAsJsonAsync("/orders", new { customerId = Guid.CreateVersion7(), lines }, Ct));
        Assert.AreEqual(HttpStatusCode.Created, status, body.ToString());
        return Guid.Parse(Text(body, "id"));
    }


    private static string Code() => "C" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

    private async Task PutCodeAsync(string code, int percent, DateTimeOffset expires, int maxUses)
    {
        var response = await s_client.PutAsJsonAsync("/discount-codes", new { code, percent, expiresOn = expires.ToString("yyyy-MM-dd"), maxUses }, Ct);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync(Ct));
    }

    private DateTimeOffset Later(int days) => s_factory.Time.GetUtcNow().AddDays(days);

    private async Task<(HttpStatusCode Status, JsonElement Body)> OrderAsync(string? code, params object[] lines) =>
        await SendAsync(await s_client.PostAsJsonAsync("/orders", new { customerId = Guid.CreateVersion7(), lines, discountCode = code }, Ct));

    [TestMethod]
    public async Task E1_ValidCode_GivesDiscountAndTotal()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 100);
        var (status, body) = await OrderAsync(code, Line(Sku(), 2, 50m));
        Assert.AreEqual(HttpStatusCode.Created, status, body.ToString());
        Assert.AreEqual(10m, Num(body, "discount"));
        Assert.AreEqual(90m, Num(body, "total"));
    }

    [TestMethod]
    public async Task E2_NoCode_HasZeroDiscount()
    {
        var (_, body) = await OrderAsync(null, Line(Sku(), 1, 5m));
        Assert.AreEqual(0m, Num(body, "discount"));
        Assert.AreEqual(5m, Num(body, "total"));
    }

    [TestMethod]
    public async Task E3_UnknownCode_Returns409()
    {
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync("NOPE" + Code(), Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task E4_ExpiredCode_Returns409()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(-1), 100);
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task E5_UsedUp_Returns409()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 1);
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H1_LastMomentOfLastDay_IsValid()
    {
        var code = Code();
        var last = new DateTimeOffset(s_factory.Time.GetUtcNow().UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
        await PutCodeAsync(code, 10, last, 100);
        s_factory.Time.SetUtcNow(last.AddDays(1).AddSeconds(-1));
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
        s_factory.Time.SetUtcNow(last.AddDays(1));
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H2_CodeComparesWithoutCaseAndSpaces()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 100);
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(" " + code.ToLowerInvariant() + " ", Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H3_RoundsOneTime_HalfAwayFromZero()
    {
        var code = Code();
        await PutCodeAsync(code, 50, Later(30), 100);
        Assert.AreEqual(0.03m, Num((await OrderAsync(code, Line(Sku(), 1, 0.05m))).Body, "discount"), "half a cent rounds away from zero");
        Assert.AreEqual(0.05m, Num((await OrderAsync(code, Line(Sku(), 1, 0.05m), Line(Sku(), 1, 0.05m))).Body, "discount"), "round the order, not each line");
    }

    [TestMethod]
    public async Task H4_ParallelOrders_NeverPassTheLimit()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 3);
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => OrderAsync(code, Line(Sku(), 1, 5m))));
        var won = results.Count(r => r.Status == HttpStatusCode.Created);
        Assert.IsTrue(won is >= 1 and <= 3, $"{won} orders with a limit of 3");

        // A request that lost a conflict gets 409, and that is correct. Each order must still have used one use.
        for (var i = won; i < 3; i++)
        {
            Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status, "a use got lost");
        }

        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status, "the limit was passed");
    }

    [TestMethod]
    public async Task H5_CancelGivesTheUseBack()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 1);
        var (_, body) = await OrderAsync(code, Line(Sku(), 1, 5m));
        (await s_client.PostAsync(new Uri($"/orders/{Text(body, "id")}/cancel", UriKind.Relative), null, Ct)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H6_SecondCancel_GivesNothingBackTwice()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 1);
        var (_, body) = await OrderAsync(code, Line(Sku(), 1, 5m));
        var cancel = new Uri($"/orders/{Text(body, "id")}/cancel", UriKind.Relative);
        (await s_client.PostAsync(cancel, null, Ct)).EnsureSuccessStatusCode();
        (await s_client.PostAsync(cancel, null, Ct)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H7_RefusedForStock_UsesNoCode()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 1);
        var sku = Sku();
        await PutStockAsync(sku, 1);
        Assert.AreEqual(HttpStatusCode.Conflict, (await OrderAsync(code, Line(sku, 5, 5m))).Status);
        Assert.AreEqual(HttpStatusCode.Created, (await OrderAsync(code, Line(Sku(), 1, 5m))).Status);
    }

    [TestMethod]
    public async Task H8_ReadAndListShowTheSameValues()
    {
        var code = Code();
        await PutCodeAsync(code, 10, Later(30), 100);
        var customerId = Guid.CreateVersion7();
        var (_, body) = await SendAsync(await s_client.PostAsJsonAsync("/orders", new { customerId, lines = new[] { Line(Sku(), 2, 50m) }, discountCode = code }, Ct));
        var read = await ReadAsync(Guid.Parse(Text(body, "id")));
        Assert.AreEqual(10m, Num(read, "discount"));
        Assert.AreEqual(90m, Num(read, "total"));
        var (_, list) = await SendAsync(await s_client.GetAsync(new Uri($"/orders?customerId={customerId}", UriKind.Relative), Ct));
        Assert.AreEqual(90m, Num(list[0], "total"));
        Assert.AreEqual(10m, Num(list[0], "discount"));
    }
}
