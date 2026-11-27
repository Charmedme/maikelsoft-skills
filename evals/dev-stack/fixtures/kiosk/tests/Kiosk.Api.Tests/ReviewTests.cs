using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class ReviewTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task List_ReturnsReviewsOfProduct()
    {
        using var client = s_factory.CreateClient();
        var id = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "LMP-100").Select(p => p.Id).SingleAsync());
        var reviews = await client.GetFromJsonAsync<JsonElement>($"/api/products/{id}/reviews", TestContext.CancellationToken);
        Assert.AreEqual(2, reviews.GetArrayLength());
    }

    [TestMethod]
    public async Task Create_UsesSignedInAuthor()
    {
        var (client, email) = await s_factory.NewCustomerAsync("reviewer");
        using (client)
        {
            var id = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "KTL-020").Select(p => p.Id).SingleAsync());
            using var response = await client.PostAsJsonAsync($"/api/products/{id}/reviews", new { message = "Boils fast.", rating = 4 }, TestContext.CancellationToken);
            var review = await response.JsonAsync();
            Assert.AreEqual(email, review.GetProperty("author").GetString());
            Assert.AreEqual(4, review.GetProperty("rating").GetInt32());
        }
    }

    [TestMethod]
    public async Task Update_OwnReview_ChangesMessage()
    {
        var (client, _) = await s_factory.NewCustomerAsync("editor");
        using (client)
        {
            var productId = await s_factory.QueryAsync(db => db.Products.Where(p => p.Sku == "BLK-300").Select(p => p.Id).SingleAsync());
            using var created = await client.PostAsJsonAsync($"/api/products/{productId}/reviews", new { message = "Warm.", rating = 5 }, TestContext.CancellationToken);
            var reviewId = (await created.JsonAsync()).GetProperty("id").GetInt32();

            using var response = await client.PatchAsJsonAsync("/api/reviews", new { id = reviewId, message = "Very warm." }, TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var message = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Id == reviewId).Select(r => r.Message).SingleAsync());
            Assert.AreEqual("Very warm.", message);
        }
    }

    [TestMethod]
    public async Task Admin_DeletesReview()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var id = await s_factory.QueryAsync(db => db.Reviews.Where(r => r.Message == "Nice glaze.").Select(r => r.Id).SingleAsync());
        using var response = await admin.DeleteAsync($"/api/admin/reviews/{id}", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
    }
}
