using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class B2bTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task XmlOrder_IsImported()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.PartnerEmail, TestApi.PartnerPassword);
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <order reference="PO-7781">
              <deliveryAddress>Retail Park 5, Breda</deliveryAddress>
              <note>Deliver to dock 2</note>
              <line sku="NTB-400" quantity="10" />
              <line sku="PEN-410" quantity="2" />
            </order>
            """;
        using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        using var response = await client.PostAsync("/api/b2b/orders", content, TestContext.CancellationToken);
        var order = await response.JsonAsync();
        Assert.AreEqual(134.80m, order.GetProperty("total").GetDecimal());
        Assert.AreEqual("Deliver to dock 2", order.GetProperty("note").GetString());
    }

    [TestMethod]
    public async Task JsonOrder_IsImported()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.PartnerEmail, TestApi.PartnerPassword);
        const string json = """
            {"reference":"PO-7782","deliveryAddress":"Retail Park 5, Breda","lines":[{"sku":"NTB-400","quantity":4}],"extensions":{"channel":"edi"}}
            """;
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/api/b2b/orders/json", content, TestContext.CancellationToken);
        var order = await response.JsonAsync();
        Assert.AreEqual(26.00m, order.GetProperty("total").GetDecimal());
    }

    [TestMethod]
    public async Task PriceList_UpdatesOwnProducts()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.PartnerEmail, TestApi.PartnerPassword);
        const string xml = """
            <prices><price sku="NTB-400" amount="6.95" /><price sku="LMP-100" amount="1.00" /></prices>
            """;
        using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
        using var response = await client.PostAsync("/api/b2b/price-list", content, TestContext.CancellationToken);
        var result = await response.JsonAsync();
        Assert.AreEqual(1, result.GetProperty("updated").GetInt32());
        var lamp = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "LMP-100").Select(p => p.Price).SingleAsync());
        Assert.AreEqual(39.95m, lamp);
    }

    [TestMethod]
    public async Task Customer_CannotImport()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        using var content = new StringContent("<order />", Encoding.UTF8, "application/xml");
        using var response = await client.PostAsync("/api/b2b/orders", content, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
