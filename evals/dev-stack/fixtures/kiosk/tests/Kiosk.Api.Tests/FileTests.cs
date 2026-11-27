using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class FileTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task InvoiceDocument_CanBeDownloaded()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        using var response = await client.GetAsync("/api/invoices/download?name=payment-terms.md", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("14 days", await response.Content.ReadAsStringAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task OrderInvoice_OfOwnOrder_IsPdf()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        var id = await s_factory.QueryAsync(db => db.Orders.Where(o => o.OrderNumber == "K-1003").Select(o => o.Id).SingleAsync());
        using var response = await client.GetAsync($"/api/orders/{id}/invoice", TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }

    [TestMethod]
    public async Task UploadedImage_IsServed()
    {
        using var client = await s_factory.SignedInClientAsync(TestApi.TomEmail, TestApi.TomPassword);
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0]);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "file", $"photo-{Guid.NewGuid():N}.png");

        using var response = await client.PostAsync("/api/uploads", form, TestContext.CancellationToken);
        var url = (await response.JsonAsync()).GetProperty("url").GetString()!;

        using var served = await s_factory.CreateClient().GetAsync(url, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, served.StatusCode);
        Assert.AreEqual("image/png", served.Content.Headers.ContentType?.MediaType);
    }
}
