using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class CatalogTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task List_ReturnsSeededProducts()
    {
        using var client = s_factory.CreateClient();
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products", TestContext.CancellationToken);
        Assert.AreEqual(8, products.GetArrayLength());
    }

    [TestMethod]
    public async Task Search_FindsProductsByName()
    {
        using var client = s_factory.CreateClient();
        var products = await client.GetFromJsonAsync<JsonElement>("/api/products/search?q=lamp", TestContext.CancellationToken);
        var names = products.EnumerateArray().Select(p => p.GetProperty("name").GetString()).ToList();
        CollectionAssert.AreEquivalent(new[] { "Desk Lamp", "Reading Lamp" }, names);
    }

    [TestMethod]
    public async Task Category_ReturnsProductsOfThatCategory()
    {
        using var client = s_factory.CreateClient();
        var products = await client.GetFromJsonAsync<JsonElement>("/api/categories/Kitchen/products", TestContext.CancellationToken);
        Assert.AreEqual(2, products.GetArrayLength());
    }

    [TestMethod]
    public async Task Get_UnknownProduct_Returns404()
    {
        using var client = s_factory.CreateClient();
        using var response = await client.GetAsync("/api/products/9999", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Update_ByAdmin_ChangesPrice()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var id = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "CSH-310").Select(p => p.Id).SingleAsync());
        using var response = await admin.PutAsJsonAsync($"/api/products/{id}", new { name = "Linen Cushion", description = "Washed linen.", price = 31.00m, imageUrl = (string?)null, stock = 10 }, TestContext.CancellationToken);
        var product = await response.JsonAsync();
        Assert.AreEqual(31.00m, product.GetProperty("price").GetDecimal());
    }
}
