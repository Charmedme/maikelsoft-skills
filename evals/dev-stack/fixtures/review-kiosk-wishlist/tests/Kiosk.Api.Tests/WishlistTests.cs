using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class WishlistTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task CreateAndAddItem_ShowsInOwnLists()
    {
        var (client, _) = await s_factory.NewCustomerAsync("wish");
        using (client)
        {
            var id = await CreateListAsync(client, "Birthday", isPublic: false);
            var productId = await ProductIdAsync("LMP-200");
            using var add = await client.PostAsJsonAsync($"/api/wishlists/{id}/items", new { productId, quantity = 1, note = "Blue please", targetPrice = 30m }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.Created, add.StatusCode);

            var lists = await client.GetFromJsonAsync<JsonElement>("/api/wishlists/mine", TestContext.CancellationToken);
            var list = lists.EnumerateArray().Single(l => l.GetProperty("id").GetInt32() == id);
            Assert.AreEqual("Reading Lamp", list.GetProperty("items")[0].GetProperty("productName").GetString());
            Assert.IsTrue(list.GetProperty("priceAlerts").GetBoolean());
        }
    }

    [TestMethod]
    public async Task AddItem_ToListOfOtherCustomer_ReturnsNotFound()
    {
        using var maria = await s_factory.SignedInClientAsync(TestApi.MariaEmail, TestApi.MariaPassword);
        using var tom = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        var id = await CreateListAsync(maria, "Garden", isPublic: false);
        using var add = await tom.PostAsJsonAsync($"/api/wishlists/{id}/items", new { productId = await ProductIdAsync("MUG-010"), quantity = 1 }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NotFound, add.StatusCode);
    }

    [TestMethod]
    public async Task ShareLink_OpensListWithoutAccount()
    {
        var (client, _) = await s_factory.NewCustomerAsync("share");
        using (client)
        {
            var id = await CreateListAsync(client, "Wedding", isPublic: false);
            using var add = await client.PostAsJsonAsync($"/api/wishlists/{id}/items", new { productId = await ProductIdAsync("BLK-300"), quantity = 2 }, TestContext.CancellationToken);
            var share = await (await client.PostAsync($"/api/wishlists/{id}/share", null, TestContext.CancellationToken)).JsonAsync();
            var url = new Uri(share.GetProperty("url").GetString()!);

            using var anonymous = s_factory.CreateClient();
            var shared = await anonymous.GetFromJsonAsync<JsonElement>(url.PathAndQuery, TestContext.CancellationToken);
            Assert.AreEqual("Wedding", shared.GetProperty("name").GetString());
            Assert.AreEqual(2, shared.GetProperty("items")[0].GetProperty("quantity").GetInt32());
        }
    }

    [TestMethod]
    public async Task SearchPublic_FindsPublicListsOnly()
    {
        var (client, _) = await s_factory.NewCustomerAsync("search");
        using (client)
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            await CreateListAsync(client, $"Holiday {tag}", isPublic: true);
            await CreateListAsync(client, $"Secret {tag}", isPublic: false);
            var found = await client.GetFromJsonAsync<JsonElement>($"/api/wishlists/search?q={tag}", TestContext.CancellationToken);
            Assert.AreEqual(1, found.GetArrayLength());
            Assert.AreEqual($"Holiday {tag}", found[0].GetProperty("name").GetString());
        }
    }

    [TestMethod]
    public async Task Export_UsesTemplateHeader()
    {
        var (client, _) = await s_factory.NewCustomerAsync("export");
        using (client)
        {
            var id = await CreateListAsync(client, "Party", isPublic: false);
            using var add = await client.PostAsJsonAsync($"/api/wishlists/{id}/items", new { productId = await ProductIdAsync("PEN-410"), quantity = 1 }, TestContext.CancellationToken);
            var text = await client.GetStringAsync($"/api/wishlists/{id}/export?template=birthday", TestContext.CancellationToken);
            StringAssert.StartsWith(text, "It is my birthday soon!");
            StringAssert.Contains(text, "1 x Fountain Pen");
        }
    }

    private async Task<int> CreateListAsync(HttpClient client, string name, bool isPublic)
    {
        using var response = await client.PostAsJsonAsync("/api/wishlists", new { name, isPublic }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        return (await response.JsonAsync()).GetProperty("id").GetInt32();
    }

    private static Task<int> ProductIdAsync(string sku) =>
        s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == sku).Select(p => p.Id).SingleAsync());
}
