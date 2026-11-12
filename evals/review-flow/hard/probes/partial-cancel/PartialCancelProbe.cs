using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Hidden tests of SHOP-60. E tests check the criteria. H tests check docs/domain.md and traps of the existing code.
[TestClass]
[DoNotParallelize]
public sealed class PartialCancelProbe
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


    private async Task<(HttpStatusCode Status, JsonElement Body)> CancelAsync(Guid order, string sku, int quantity) =>
        await SendAsync(await s_client.PostAsJsonAsync($"/orders/{order}/lines/{sku}/cancel", new { quantity }, Ct));

    private static JsonElement? FindLine(JsonElement order, string sku)
    {
        foreach (var line in order.EnumerateObject().First(p => p.Name.Equals("lines", StringComparison.OrdinalIgnoreCase)).Value.EnumerateArray())
        {
            if (Text(line, "sku") == sku)
            {
                return line;
            }
        }

        return null;
    }

    [TestMethod]
    public async Task E1_PartOfLine_LowersQuantityAndTotal()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 5, 2m));
        var (status, body) = await CancelAsync(order, sku, 2);
        Assert.AreEqual(HttpStatusCode.OK, status, body.ToString());
        Assert.AreEqual(3m, Num(FindLine(body, sku)!.Value, "quantity"));
        Assert.AreEqual(6m, Num(body, "total"));
        Assert.AreEqual(6m, Num(await ReadAsync(order), "total"));
    }

    [TestMethod]
    public async Task E2_FullLine_RemovesItAndKeepsTheOrder()
    {
        string a = Sku(), b = Sku();
        var order = await PlaceAsync(Line(a, 1, 2m), Line(b, 1, 3m));
        var (status, body) = await CancelAsync(order, a, 1);
        Assert.AreEqual(HttpStatusCode.OK, status, body.ToString());
        Assert.IsNull(FindLine(body, a));
        Assert.AreEqual("Placed", Text(body, "status"));
    }

    [TestMethod]
    public async Task E3_LastLine_CancelsTheOrder()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 2, 2m));
        var (_, body) = await CancelAsync(order, sku, 2);
        Assert.AreEqual("Cancelled", Text(body, "status"));
        Assert.AreEqual("Cancelled", Text(await ReadAsync(order), "status"));
    }

    [TestMethod]
    public async Task E4_ShippedOrder_Returns409()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 2, 2m));
        (await s_client.PostAsync(new Uri($"/orders/{order}/ship", UriKind.Relative), null, Ct)).EnsureSuccessStatusCode();
        Assert.AreEqual(HttpStatusCode.Conflict, (await CancelAsync(order, sku, 1)).Status);
    }

    [TestMethod]
    public async Task E5_AfterWindow_Returns409()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 2, 2m));
        s_factory.Time.Advance(TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1));
        Assert.AreEqual(HttpStatusCode.Conflict, (await CancelAsync(order, sku, 1)).Status);
    }

    [TestMethod]
    public async Task E6_Unknown_Returns404()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 2, 2m));
        Assert.AreEqual(HttpStatusCode.NotFound, (await CancelAsync(Guid.CreateVersion7(), sku, 1)).Status);
        Assert.AreEqual(HttpStatusCode.NotFound, (await CancelAsync(order, Sku(), 1)).Status);
    }

    [TestMethod]
    public async Task E7_TooMany_Returns409AndNoChange_BelowOne_Returns400()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await PlaceAsync(Line(sku, 2, 2m));
        Assert.AreEqual(HttpStatusCode.Conflict, (await CancelAsync(order, sku, 3)).Status);
        Assert.AreEqual(HttpStatusCode.BadRequest, (await CancelAsync(order, sku, 0)).Status);
        Assert.AreEqual(2m, Num(FindLine(await ReadAsync(order), sku)!.Value, "quantity"));
        Assert.AreEqual(8, await StockAsync(sku));
    }

    [TestMethod]
    public async Task H1_ExactEndOfWindow_IsInside()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 2, 2m));
        s_factory.Time.Advance(TimeSpan.FromHours(24));
        Assert.AreEqual(HttpStatusCode.OK, (await CancelAsync(order, sku, 1)).Status);
    }

    [TestMethod]
    public async Task H2_PartialCancel_ReleasesStock()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await PlaceAsync(Line(sku, 4, 2m));
        await CancelAsync(order, sku, 3);
        Assert.AreEqual(9, await StockAsync(sku));
    }

    [TestMethod]
    public async Task H3_PartialCancel_WritesAnEvent()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 4, 2m));
        var before = await EventsAsync(order);
        await CancelAsync(order, sku, 1);
        Assert.IsTrue(await EventsAsync(order) > before);
    }

    [TestMethod]
    public async Task H4_PartThenWholeCancel_ReleasesEachUnitOneTime()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await PlaceAsync(Line(sku, 4, 2m));
        await CancelAsync(order, sku, 1);
        (await s_client.PostAsync(new Uri($"/orders/{order}/cancel", UriKind.Relative), null, Ct)).EnsureSuccessStatusCode();
        Assert.AreEqual(10, await StockAsync(sku));
    }

    [TestMethod]
    public async Task H5_LastLine_ReleasesStockAndWritesEvent()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await PlaceAsync(Line(sku, 2, 2m));
        var before = await EventsAsync(order);
        await CancelAsync(order, sku, 2);
        Assert.AreEqual(10, await StockAsync(sku));
        Assert.IsTrue(await EventsAsync(order) > before);
    }

    [TestMethod]
    public async Task H6_ParallelCancels_LoseNothingAndDoubleNothing()
    {
        var sku = Sku();
        await PutStockAsync(sku, 50);
        var order = await PlaceAsync(Line(sku, 10, 2m));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => CancelAsync(order, sku, 1)));
        var won = results.Count(r => r.Status == HttpStatusCode.OK);
        var left = (int)Num(FindLine(await ReadAsync(order), sku)!.Value, "quantity");
        Assert.AreEqual(10 - won, left, "the quantity must fall by one for each 200");
        Assert.AreEqual(50 - left, await StockAsync(sku), "stock plus the reserved quantity must stay 50");
    }
}
