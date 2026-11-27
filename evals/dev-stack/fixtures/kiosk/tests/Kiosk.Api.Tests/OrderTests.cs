using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class OrderTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Create_ComputesTotal()
    {
        var (client, _) = await s_factory.NewCustomerAsync("order");
        using (client)
        {
            var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "PEN-410").Select(p => p.Id).SingleAsync());
            using var response = await client.PostAsJsonAsync("/api/orders", new { lines = new[] { new { productId, quantity = 2 } }, deliveryAddress = "Kade 2, Delft" }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var order = await response.JsonAsync();
            Assert.AreEqual(69.80m, order.GetProperty("total").GetDecimal());
            Assert.AreEqual("AwaitingPayment", order.GetProperty("status").GetString());
        }
    }

    [TestMethod]
    public async Task ListMine_ReturnsOnlyOwnOrders()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.MariaEmail, TestApi.MariaPassword);
        var orders = await client.GetFromJsonAsync<JsonElement>("/api/orders", TestContext.CancellationToken);
        var numbers = orders.EnumerateArray().Select(o => o.GetProperty("orderNumber").GetString()).ToList();
        CollectionAssert.IsSubsetOf(new[] { "K-1001", "K-1002" }, numbers);
        CollectionAssert.DoesNotContain(numbers, "K-1003");
    }

    [TestMethod]
    public async Task WebSession_ReadsOwnOrder()
    {
        using var client = s_factory.CookieClient();
        using var signIn = await TestApi.CookieSignInAsync(client, TestApi.MariaEmail, TestApi.MariaPassword, "/account");
        Assert.AreEqual(HttpStatusCode.Redirect, signIn.StatusCode);
        var id = await s_factory.QueryAsync(db => db.Orders.Where(o => o.OrderNumber == "K-1002").Select(o => o.Id).SingleAsync());

        var order = await client.GetFromJsonAsync<JsonElement>($"/api/orders/{id}", TestContext.CancellationToken);

        Assert.AreEqual("K-1002", order.GetProperty("orderNumber").GetString());
    }

    [TestMethod]
    public async Task GuestCheckout_CreatesOrderWithoutAccount()
    {
        using var client = s_factory.CreateClient();
        var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "MUG-010").Select(p => p.Id).SingleAsync());
        using var response = await client.PostAsJsonAsync("/api/orders/guest", new { email = TestApi.NewEmail("guest"), lines = new[] { new { productId, quantity = 1 } }, deliveryAddress = "Plein 3, Gouda" }, TestContext.CancellationToken);
        var order = await response.JsonAsync();
        var number = order.GetProperty("orderNumber").GetString();
        var owner = await s_factory.QueryAsync(db => db.Orders.Where(o => o.OrderNumber == number).Select(o => o.CustomerId).SingleAsync());
        Assert.IsNull(owner);
    }

    [TestMethod]
    public async Task ReportDamage_OnDeliveredOrder_ChangesStatus()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        var id = await s_factory.QueryAsync(db => db.Orders.Where(o => o.OrderNumber == "K-1003").Select(o => o.Id).SingleAsync());
        using var response = await client.PostAsync($"/api/orders/{id}/report-damage", null, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
}
