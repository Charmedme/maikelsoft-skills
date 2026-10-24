using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Products;
using Orders.Api.Stock;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProductSearchAndAlertTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Search_IgnoresCaseAndAccents()
    {
        await CreateProductAsync("SKU-CAFE", "Café Crème Mug");
        await CreateProductAsync("SKU-TEA", "Tea Pot");

        var found = await s_client.GetFromJsonAsync<List<ProductResponse>>("/products/search?q=cafe%20creme", Ct);

        found.Should().ContainSingle().Which.Sku.Should().Be("SKU-CAFE");
    }

    [TestMethod]
    public async Task Search_PercentSign_IsLiteral()
    {
        await CreateProductAsync("SKU-SALE", "Mug 50% off");
        await CreateProductAsync("SKU-MUG5", "Mug 500 ml");

        var found = await s_client.GetFromJsonAsync<List<ProductResponse>>("/products/search?q=50%25", Ct);

        found.Should().ContainSingle().Which.Sku.Should().Be("SKU-SALE");
    }

    [TestMethod]
    public async Task Subscribe_Twice_KeepsOneAlert()
    {
        await CreateProductAsync("SKU-ALERT-1", "Lamp");
        var customerId = Guid.CreateVersion7();

        var first = await s_client.PostAsJsonAsync("/products/SKU-ALERT-1/alerts", new CreateAlertRequest(customerId), Ct);
        var second = await s_client.PostAsJsonAsync("/products/SKU-ALERT-1/alerts", new CreateAlertRequest(customerId), Ct);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadFromJsonAsync<AlertResponse>(Ct))!.CreatedAt
            .Should().Be((await first.Content.ReadFromJsonAsync<AlertResponse>(Ct))!.CreatedAt);
    }

    [TestMethod]
    public async Task SetStock_FromZero_MarksAlerts()
    {
        await CreateProductAsync("SKU-ALERT-2", "Chair");
        await s_client.PutAsJsonAsync("/stock/SKU-ALERT-2", new SetStockRequest(0), Ct);
        var customerId = Guid.CreateVersion7();
        await s_client.PostAsJsonAsync("/products/SKU-ALERT-2/alerts", new CreateAlertRequest(customerId), Ct);

        await s_client.PutAsJsonAsync("/stock/SKU-ALERT-2", new SetStockRequest(4), Ct);

        var again = await s_client.PostAsJsonAsync("/products/SKU-ALERT-2/alerts", new CreateAlertRequest(customerId), Ct);
        (await again.Content.ReadFromJsonAsync<AlertResponse>(Ct))!.Notified.Should().BeTrue();
    }

    [TestMethod]
    public async Task Subscribe_UnknownProduct_ReturnsNotFound()
    {
        var response = await s_client.PostAsJsonAsync("/products/NOPE/alerts", new CreateAlertRequest(Guid.CreateVersion7()), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task CreateProductAsync(string sku, string name)
    {
        var response = await s_client.PostAsJsonAsync("/products", new CreateProductRequest(sku, name, 9.95m), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
