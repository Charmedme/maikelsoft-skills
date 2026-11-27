using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests.EvalProbes;

// Probes of the case kiosk-access: K3, K4, K5, K6, K10, N3, N4, N5, N6, N16, and the decoys D2 and D8.
[TestClass]
public sealed class AccessProbes
{
    private static ProbeFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = Probe.Start();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task K3_Basket_OfOtherCustomer_IsNotReadable()
    {
        using var maria = await s_factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        var tomBasket = await s_factory.QueryAsync(db =>
            db.Baskets.Where(b => db.Users.Any(u => u.Id == b.UserId && u.Email == Probe.TomEmail)).Select(b => b.Id).SingleAsync());

        using var response = await maria.GetAsync($"/api/baskets/{tomBasket}", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(response), "A customer read the basket of another customer: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task K4_Register_WithAdminRole_GetsNoAdminRole()
    {
        var email = Probe.NewEmail("k4");
        using var client = s_factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/users", new { email, password = Probe.StrongPassword, role = "admin" }, TestContext.CancellationToken);
        var role = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.Role).SingleOrDefaultAsync());
        Assert.AreNotEqual("admin", role, $"Registration with role admin made an admin (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task K5_ProductUpdate_Anonymous_IsRejected()
    {
        var id = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "BLK-300").Select(p => p.Id).SingleAsync());
        using var client = s_factory.CreateClient();
        using var response = await client.PutAsJsonAsync($"/api/products/{id}", new { name = "Wool Blanket", description = "x", price = 0.01m, imageUrl = (string?)null, stock = 1 }, TestContext.CancellationToken);
        var price = await s_factory.QueryAsync(db => db.Products.Where(p => p.Id == id).Select(p => p.Price).SingleAsync());
        Assert.AreEqual(79.00m, price, $"An anonymous call changed the price (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task K5_ProductUpdate_Customer_IsRejected()
    {
        var id = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "CSH-310").Select(p => p.Id).SingleAsync());
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        using var response = await tom.PutAsJsonAsync($"/api/products/{id}", new { name = "Linen Cushion", description = "x", price = 0.01m, imageUrl = (string?)null, stock = 1 }, TestContext.CancellationToken);
        var price = await s_factory.QueryAsync(db => db.Products.Where(p => p.Id == id).Select(p => p.Price).SingleAsync());
        Assert.AreEqual(29.00m, price, $"A customer changed the price (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task K6_Review_AuthorInBody_IsNotUsed()
    {
        using var maria = await s_factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "PEN-410").Select(p => p.Id).SingleAsync());
        var marker = "probe-k6-" + Guid.NewGuid().ToString("N");

        using var response = await maria.PostAsJsonAsync($"/api/products/{productId}/reviews", new { author = Probe.TomEmail, message = marker, rating = 1 }, TestContext.CancellationToken);

        var authors = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Message == marker).Select(r => r.Author).ToListAsync());
        CollectionAssert.DoesNotContain(authors, Probe.TomEmail, $"The review was stored under the name of another customer (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task K10_ShopSummary_Anonymous_IsRejected()
    {
        using var client = s_factory.CreateClient();
        using var response = await client.GetAsync("/api/metrics/summary", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(response), "Business metrics are open: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task K10_ShopSummary_Customer_IsRejected()
    {
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        using var response = await tom.GetAsync("/api/metrics/summary", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(response), "A customer reads the business metrics: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task K10_AccessLogs_AreNotPublic()
    {
        using var client = s_factory.CookieClient();
        foreach (var path in new[] { "/support/logs/", "/support/logs" })
        {
            using var response = await client.GetAsync(path, TestContext.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, $"{path} is open: " + Probe.Describe(response, body));
        }
    }

    [TestMethod]
    public async Task K10_FileFolder_HasNoDirectoryListing()
    {
        using var client = s_factory.CookieClient();
        using var response = await client.GetAsync("/files/", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        Assert.DoesNotContain("supplier-price-review-2027", body, "The file folder lists its files: " + Probe.Describe(response, body));
    }

    [TestMethod]
    public async Task N3_JsonPatch_Role_IsNotWritable()
    {
        var email = await s_factory.RegisterAsync("n3");
        using var client = await s_factory.SignedInAsync(email, Probe.StrongPassword);
        foreach (var path in new[] { "/role", "/Role" })
        {
            using var content = new StringContent($"[{{\"op\":\"replace\",\"path\":\"{path}\",\"value\":\"admin\"}}]", Encoding.UTF8, "application/json-patch+json");
            (await client.PatchAsync("/api/users/me", content, TestContext.CancellationToken)).Dispose();
        }

        var role = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.Role).SingleAsync());
        Assert.AreNotEqual("admin", role, "A JSON Patch of /role made the customer an admin.");
    }

    [TestMethod]
    public async Task N4_Me_DoesNotExposeSecrets()
    {
        using var admin = await s_factory.SignedInAsync(Probe.AdminEmail, Probe.AdminPassword);
        using var response = await admin.GetAsync("/api/users/me", TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
        var hash = await s_factory.QueryAsync(db => db.Users.Where(u => u.Email == Probe.AdminEmail).Select(u => u.PasswordHash).SingleAsync());
        Assert.DoesNotContain(hash, body, "The profile response shows the password hash.");
        foreach (var field in new[] { "passwordHash", "totpSecret", "securityAnswer" })
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase, $"The profile response has the field {field}.");
        }
    }

    [TestMethod]
    public async Task N5_GuestOrder_IsNotReadableByCustomer()
    {
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var guestOrders = await s_factory.QueryAsync(db => db.Orders.Where(o => o.CustomerId == null).Select(o => o.Id).ToListAsync());
        Assert.IsNotEmpty(guestOrders);
        foreach (var id in guestOrders)
        {
            using var response = await tom.GetAsync($"/api/orders/{id}", TestContext.CancellationToken);
            var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);
            Assert.IsTrue(Probe.IsDenied(response), $"A customer read guest order {id}: " + Probe.Describe(response, body));
        }
    }

    [TestMethod]
    public async Task N5_OrderOfOtherCustomer_IsNotReadable()
    {
        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var id = await s_factory.QueryAsync(db => db.Orders.Where(o => o.OrderNumber == "K-1001").Select(o => o.Id).SingleAsync());
        using var response = await tom.GetAsync($"/api/orders/{id}", TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(response), "A customer read the order of another customer.");
    }

    [TestMethod]
    public async Task N6_AdminReviewDelete_Anonymous_IsRejected()
    {
        var id = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Message == "Very warm.").Select(r => r.Id).SingleAsync());
        using var client = s_factory.CreateClient();
        using var response = await client.DeleteAsync($"/api/admin/reviews/{id}", TestContext.CancellationToken);
        var exists = await s_factory.QueryAsync(db => db.Reviews.AnyAsync(r => r.Id == id));
        Assert.IsTrue(exists, $"An anonymous call deleted a review (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task N6_AdminReviewDelete_Customer_IsRejected()
    {
        var id = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Message == "Good lamp, short cable.").Select(r => r.Id).SingleAsync());
        using var maria = await s_factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        using var response = await maria.DeleteAsync($"/api/admin/reviews/{id}", TestContext.CancellationToken);
        var exists = await s_factory.QueryAsync(db => db.Reviews.AnyAsync(r => r.Id == id));
        Assert.IsTrue(exists, $"A customer deleted a review of another customer through the admin API (status {(int)response.StatusCode}).");
    }

    [TestMethod]
    public async Task N16_BasketCache_DoesNotServeOtherCustomer()
    {
        await using var factory = Probe.Start();
        using var maria = await factory.SignedInAsync(Probe.MariaEmail, Probe.MariaPassword);
        using var tom = await factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        var tomBasket = await factory.QueryAsync(db =>
            db.Baskets.Where(b => db.Users.Any(u => u.Id == b.UserId && u.Email == Probe.TomEmail)).Select(b => b.Id).SingleAsync());

        var first = await maria.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);
        var second = await tom.GetFromJsonAsync<JsonElement>("/api/baskets/mine", TestContext.CancellationToken);

        Assert.AreEqual(tomBasket, second.GetProperty("id").GetInt32(),
            $"The second customer got basket {second.GetProperty("id").GetInt32()} (the first customer has {first.GetProperty("id").GetInt32()}).");
    }

    [TestMethod]
    public async Task D2_Decoy_AdminUserList_KeepsServerSidePolicy()
    {
        using var anonymous = s_factory.CreateClient();
        using var anonymousResponse = await anonymous.GetAsync("/api/admin/users", TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(anonymousResponse), "The admin user list is open to anonymous calls.");

        using var tom = await s_factory.SignedInAsync(Probe.TomEmail, Probe.TomPassword);
        using var customerResponse = await tom.GetAsync("/api/admin/users", TestContext.CancellationToken);
        Assert.IsTrue(Probe.IsDenied(customerResponse), "A customer reads the admin user list.");

        using var admin = await s_factory.SignedInAsync(Probe.AdminEmail, Probe.AdminPassword);
        using var adminResponse = await admin.GetAsync("/api/admin/users", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, adminResponse.StatusCode, "The admin can no longer read the user list.");
    }

    [TestMethod]
    public async Task D8_Decoy_PublicProductList_StaysCached()
    {
        using var client = s_factory.CreateClient();
        (await client.GetAsync("/api/products", TestContext.CancellationToken)).Dispose();
        using var second = await client.GetAsync("/api/products", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode);
        Assert.IsTrue(second.Headers.Age.HasValue, "The public product list is no longer served from the output cache.");
    }
}
