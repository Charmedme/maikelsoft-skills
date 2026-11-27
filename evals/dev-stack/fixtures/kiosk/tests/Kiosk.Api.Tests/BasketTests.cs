using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class BasketTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Mine_ReturnsOwnBasket()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.MariaEmail, TestApi.MariaPassword);
        var basket = await client.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);
        var items = basket.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()).ToList();
        CollectionAssert.Contains(items, "Desk Lamp");
    }

    [TestMethod]
    public async Task AddItem_ShowsInBasket()
    {
        var (client, _) = await s_factory.NewCustomerAsync("basket");
        using (client)
        {
            _ = await client.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);
            var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "MUG-010").Select(p => p.Id).SingleAsync());
            using var add = await client.PostAsJsonAsync("/api/baskets/mine/items", new { productId, quantity = 2 }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, add.StatusCode);

            var basket = await client.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);
            var item = basket.GetProperty("items").EnumerateArray().Single();
            Assert.AreEqual(2, item.GetProperty("quantity").GetInt32());
            Assert.AreEqual(19.90m, basket.GetProperty("total").GetDecimal());
        }
    }

    [TestMethod]
    public async Task Checkout_CreatesOrderAndEmptiesBasket()
    {
        var (client, _) = await s_factory.NewCustomerAsync("checkout");
        using (client)
        {
            var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "NTB-400").Select(p => p.Id).SingleAsync());
            (await client.PostAsJsonAsync("/api/baskets/mine/items", new { productId, quantity = 3 }, TestContext.CancellationToken)).Dispose();
            using var checkout = await client.PostAsJsonAsync("/api/baskets/mine/checkout", new { deliveryAddress = "Kade 1, Delft" }, TestContext.CancellationToken);
            var order = await checkout.JsonAsync();
            Assert.AreEqual(19.50m, order.GetProperty("total").GetDecimal());

            var basket = await client.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);
            Assert.AreEqual(0, basket.GetProperty("items").GetArrayLength());
        }
    }
}
