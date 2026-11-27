using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Tests;

[TestClass]
public sealed class AdminTests
{
    private static KioskApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void Initialize(TestContext context) => s_factory = new KioskApiFactory();

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Admin_ListsUsers()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var users = await admin.GetFromJsonAsync<JsonElement>("/api/admin/users", TestContext.CancellationToken);
        Assert.IsGreaterThanOrEqualTo(5, users.GetArrayLength());
    }

    [TestMethod]
    public async Task Admin_FiltersProductsBySupplier()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var products = await admin.GetFromJsonAsync<JsonElement>("/api/admin/products?supplier=supply@northloom.test", TestContext.CancellationToken);
        Assert.AreEqual(2, products.GetArrayLength());
    }

    [TestMethod]
    public async Task Admin_CreatesProduct()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        using var response = await admin.PostAsJsonAsync("/api/admin/products", new
        {
            name = "Desk Organizer",
            description = "Bamboo tray.",
            price = 18.00m,
            category = "Office",
            sku = "ORG-420",
            supplierEmail = "orders@partner-retail.test",
            stock = 12,
        }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
    }

    [TestMethod]
    public async Task Accounting_ListsOrders()
    {
        using var finance = await s_factory.SignedInClientAsync(TestApi.FinanceEmail, TestApi.FinancePassword);
        var orders = await finance.GetFromJsonAsync<JsonElement>("/api/admin/orders", TestContext.CancellationToken);
        Assert.IsGreaterThanOrEqualTo(5, orders.GetArrayLength());
    }

    [TestMethod]
    public async Task Admin_ReadsShopSummary()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var summary = await admin.GetFromJsonAsync<JsonElement>("/api/metrics/summary", TestContext.CancellationToken);
        Assert.IsGreaterThanOrEqualTo(5, summary.GetProperty("orders").GetInt32());
    }

    [TestMethod]
    public async Task Admin_SchedulesExport()
    {
        using var admin = await s_factory.SignedInClientAsync(TestApi.AdminEmail, TestApi.AdminPassword);
        var folder = Path.Combine(Path.GetTempPath(), "kiosk-exports-" + Guid.NewGuid().ToString("N"));
        using var response = await admin.PostAsJsonAsync("/api/admin/exports", new { name = "orders", cron = "0 4 * * *", archivePath = folder }, TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(Directory.Exists(folder));
        Directory.Delete(folder);
    }

    [TestMethod]
    public async Task Health_IsHealthy()
    {
        using var client = s_factory.CreateClient();
        Assert.AreEqual("Healthy", await client.GetStringAsync("/health", TestContext.CancellationToken));
    }
}
