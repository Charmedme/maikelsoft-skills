using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Time.Testing;

namespace Orders.Api.Tests.EvalProbes;

// Hidden tests of SHOP-62. E tests check the criteria. H tests check docs/domain.md and traps of the existing code.
[TestClass]
[DoNotParallelize]
public sealed class ReturnsProbe
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


    private async Task<Guid> ShippedAsync(params object[] lines)
    {
        var order = await PlaceAsync(lines);
        (await s_client.PostAsync(new Uri($"/orders/{order}/ship", UriKind.Relative), null, Ct)).EnsureSuccessStatusCode();
        return order;
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> ReturnAsync(Guid order, string? key, params (string Sku, int Quantity)[] lines)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/orders/{order}/returns")
        {
            Content = JsonContent.Create(new { lines = lines.Select(l => new { sku = l.Sku, quantity = l.Quantity }) }),
        };
        if (key is not null)
        {
            request.Headers.Add("Idempotency-Key", key);
        }

        return await SendAsync(await s_client.SendAsync(request, Ct));
    }

    private static string Key() => Guid.NewGuid().ToString("N");

    [TestMethod]
    public async Task E1_Return_GivesRefundMinusFee()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        var (status, body) = await ReturnAsync(order, Key(), (sku, 2));
        Assert.AreEqual(HttpStatusCode.OK, status, body.ToString());
        Assert.AreEqual(18m, Num(body, "refund"));
    }

    [TestMethod]
    public async Task E2_Refused_NotShippedUnknownSkuUnknownOrder()
    {
        var sku = Sku();
        var placed = await PlaceAsync(Line(sku, 3, 10m));
        Assert.AreEqual(HttpStatusCode.Conflict, (await ReturnAsync(placed, Key(), (sku, 1))).Status);
        var shipped = await ShippedAsync(Line(sku, 3, 10m));
        Assert.AreEqual(HttpStatusCode.Conflict, (await ReturnAsync(shipped, Key(), (Sku(), 1))).Status);
        Assert.AreEqual(HttpStatusCode.NotFound, (await ReturnAsync(Guid.CreateVersion7(), Key(), (sku, 1))).Status);
    }

    [TestMethod]
    public async Task E3_AfterWindow_Returns409()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        s_factory.Time.Advance(TimeSpan.FromDays(14) + TimeSpan.FromSeconds(1));
        Assert.AreEqual(HttpStatusCode.Conflict, (await ReturnAsync(order, Key(), (sku, 1))).Status);
    }

    [TestMethod]
    public async Task E4_NoKey_Returns400_SameKey_SameResult()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await ShippedAsync(Line(sku, 3, 10m));
        Assert.AreEqual(HttpStatusCode.BadRequest, (await ReturnAsync(order, null, (sku, 1))).Status);
        var key = Key();
        var first = await ReturnAsync(order, key, (sku, 1));
        var second = await ReturnAsync(order, key, (sku, 1));
        Assert.AreEqual(HttpStatusCode.OK, second.Status);
        Assert.AreEqual(Num(first.Body, "refund"), Num(second.Body, "refund"));
        var (_, list) = await SendAsync(await s_client.GetAsync(new Uri($"/orders/{order}/returns", UriKind.Relative), Ct));
        Assert.AreEqual(1, list.GetArrayLength());
    }

    [TestMethod]
    public async Task E5_TooMuch_Returns409()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 2, 10m));
        Assert.AreEqual(HttpStatusCode.Conflict, (await ReturnAsync(order, Key(), (sku, 3))).Status);
    }

    [TestMethod]
    public async Task E6_OrderStaysAsItIs_AndListHasTheRefund()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        await ReturnAsync(order, Key(), (sku, 1));
        var read = await ReadAsync(order);
        Assert.AreEqual(30m, Num(read, "total"));
        Assert.AreEqual("Shipped", Text(read, "status"));
        var (_, list) = await SendAsync(await s_client.GetAsync(new Uri($"/orders/{order}/returns", UriKind.Relative), Ct));
        Assert.AreEqual(9m, Num(list[0], "refund"));
    }

    [TestMethod]
    public async Task H1_ExactEndOfWindow_IsInside()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        s_factory.Time.Advance(TimeSpan.FromDays(14));
        Assert.AreEqual(HttpStatusCode.OK, (await ReturnAsync(order, Key(), (sku, 1))).Status);
    }

    [TestMethod]
    public async Task H2_WindowCountsFromShipping_NotFromPlacing()
    {
        var sku = Sku();
        var order = await PlaceAsync(Line(sku, 3, 10m));
        s_factory.Time.Advance(TimeSpan.FromDays(10));
        (await s_client.PostAsync(new Uri($"/orders/{order}/ship", UriKind.Relative), null, Ct)).EnsureSuccessStatusCode();
        s_factory.Time.Advance(TimeSpan.FromDays(10));
        Assert.AreEqual(HttpStatusCode.OK, (await ReturnAsync(order, Key(), (sku, 1))).Status);
    }

    [TestMethod]
    public async Task H3_TwoReturns_CountTogether()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        Assert.AreEqual(HttpStatusCode.OK, (await ReturnAsync(order, Key(), (sku, 2))).Status);
        Assert.AreEqual(HttpStatusCode.Conflict, (await ReturnAsync(order, Key(), (sku, 2))).Status);
    }

    [TestMethod]
    public async Task H4_Return_ReleasesStock()
    {
        var sku = Sku();
        await PutStockAsync(sku, 10);
        var order = await ShippedAsync(Line(sku, 3, 10m));
        await ReturnAsync(order, Key(), (sku, 2));
        Assert.AreEqual(9, await StockAsync(sku));
    }

    [TestMethod]
    public async Task H5_Return_WritesAnEvent()
    {
        var sku = Sku();
        var order = await ShippedAsync(Line(sku, 3, 10m));
        var before = await EventsAsync(order);
        await ReturnAsync(order, Key(), (sku, 1));
        Assert.IsTrue(await EventsAsync(order) > before);
    }

    [TestMethod]
    public async Task H6_RoundsOneTime_HalfAwayFromZero()
    {
        string a = Sku(), b = Sku();
        var order = await ShippedAsync(Line(a, 1, 0.05m), Line(b, 1, 0.05m));
        Assert.AreEqual(0.05m, Num((await ReturnAsync(order, Key(), (a, 1))).Body, "refund"), "0.045 rounds away from zero");
        var other = await ShippedAsync(Line(a, 1, 0.05m), Line(b, 1, 0.05m));
        Assert.AreEqual(0.09m, Num((await ReturnAsync(other, Key(), (a, 1), (b, 1))).Body, "refund"), "round the return, not each line");
    }

    [TestMethod]
    public async Task H7_ParallelReturns_NeverPassTheQuantity()
    {
        var sku = Sku();
        await PutStockAsync(sku, 20);
        var order = await ShippedAsync(Line(sku, 3, 10m));
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => ReturnAsync(order, Key(), (sku, 1))));
        var won = results.Count(r => r.Status == HttpStatusCode.OK);
        Assert.IsTrue(won <= 3, $"{won} returns of a quantity of 3");
        Assert.AreEqual(17 + won, await StockAsync(sku), "stock must rise by one for each 200");
    }

    [TestMethod]
    public async Task H8_ParallelSameKey_RecordsOneReturn()
    {
        var sku = Sku();
        await PutStockAsync(sku, 20);
        var order = await ShippedAsync(Line(sku, 5, 10m));
        var key = Key();
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => ReturnAsync(order, key, (sku, 1))));
        var (_, list) = await SendAsync(await s_client.GetAsync(new Uri($"/orders/{order}/returns", UriKind.Relative), Ct));
        Assert.AreEqual(1, list.GetArrayLength());
        Assert.AreEqual(16, await StockAsync(sku));
    }
}
