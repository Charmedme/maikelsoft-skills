using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for the dev-team story ORD-52 (export the orders of a customer as CSV).
// E* tests check the examples of the item. H* tests check the rules with values that no example gives.
[TestClass]
[DoNotParallelize]
public sealed class OrderExportProbe
{
    private const string Header = "id,placedAt,status,sku,quantity,unitPrice,lineTotal";
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    private async Task<Guid> PlaceAsync(Guid customer, params (string Sku, int Quantity, decimal UnitPrice)[] lines)
    {
        var request = new { customerId = customer, lines = lines.Select(l => new { sku = l.Sku, quantity = l.Quantity, unitPrice = l.UnitPrice }).ToArray() };
        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, text);
        return JsonDocument.Parse(text).RootElement.GetProperty("id").GetGuid();
    }

    private async Task<(HttpResponseMessage Response, string[] Lines)> ExportAsync(Guid customer)
    {
        var response = await s_client.GetAsync($"/orders/export?customerId={customer}", TestContext.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var lines = text.TrimStart('﻿').Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        return (response, lines);
    }

    private static void AssertRow(string expected, string[] lines, Guid id)
    {
        var want = expected.Replace("<id>", id.ToString());
        Assert.IsTrue(lines.Skip(1).Any(l => l.Equals(want, StringComparison.OrdinalIgnoreCase)),
            $"Row not found: {want}\nFile:\n{string.Join("\n", lines)}");
    }

    [TestMethod]
    public async Task E1_TwoLines_HeaderAndRows()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("SKU-1", 2, 2.5m), ("SKU-2", 1, 1m));
        var (response, lines) = await ExportAsync(customer);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual(Header, lines[0]);
        Assert.AreEqual(3, lines.Length, string.Join("\n", lines));
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,SKU-1,2,2.50,5.00", lines, id);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,SKU-2,1,1.00,1.00", lines, id);
    }

    [TestMethod]
    public async Task E2_CommaInSku_IsQuoted()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("BOX,SMALL", 1, 3m));
        var (_, lines) = await ExportAsync(customer);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,\"BOX,SMALL\",1,3.00,3.00", lines, id);
    }

    [TestMethod]
    public async Task E3_FormulaInSku_GetsAQuote()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("=1+1", 1, 3m));
        var (_, lines) = await ExportAsync(customer);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,'=1+1,1,3.00,3.00", lines, id);
    }

    [TestMethod]
    public async Task E4_NoOrders_OnlyHeader_EmptyId400()
    {
        var (response, lines) = await ExportAsync(Guid.CreateVersion7());
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(1, lines.Length, string.Join("\n", lines));
        Assert.AreEqual(Header, lines[0]);
        var (bad, _) = await ExportAsync(Guid.Empty);
        Assert.AreEqual(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [TestMethod]
    public async Task H1_DutchServerCulture_StillUsesADot()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("SKU-9", 3, 1.25m));
        var before = (CultureInfo.CurrentCulture, CultureInfo.DefaultThreadCurrentCulture);
        try
        {
            var nl = CultureInfo.GetCultureInfo("nl-NL");
            CultureInfo.CurrentCulture = nl;
            CultureInfo.DefaultThreadCurrentCulture = nl;
            var (_, lines) = await ExportAsync(customer);
            AssertRow("<id>,2026-10-16T09:00:00Z,Placed,SKU-9,3,1.25,3.75", lines, id);
        }
        finally
        {
            CultureInfo.CurrentCulture = before.CurrentCulture;
            CultureInfo.DefaultThreadCurrentCulture = before.DefaultThreadCurrentCulture;
        }
    }

    [TestMethod]
    public async Task H2_DoubleQuoteInSku_IsDoubled()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("SA\"M", 1, 2m));
        var (_, lines) = await ExportAsync(customer);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,\"SA\"\"M\",1,2.00,2.00", lines, id);
    }

    [TestMethod]
    public async Task H3_AtAndMinus_GetAQuote()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("@SUM(A1)", 1, 2m), ("-5", 1, 2m));
        var (_, lines) = await ExportAsync(customer);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,'@SUM(A1),1,2.00,2.00", lines, id);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,'-5,1,2.00,2.00", lines, id);
    }

    [TestMethod]
    public async Task H4_FormulaWithComma_GetsQuoteAndIsQuoted()
    {
        var customer = Guid.CreateVersion7();
        var id = await PlaceAsync(customer, ("=SUM(A1,B1)", 1, 2m));
        var (_, lines) = await ExportAsync(customer);
        AssertRow("<id>,2026-10-16T09:00:00Z,Placed,\"'=SUM(A1,B1)\",1,2.00,2.00", lines, id);
    }
}
