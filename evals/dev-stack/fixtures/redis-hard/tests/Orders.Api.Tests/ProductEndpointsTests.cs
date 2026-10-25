using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Catalog;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProductEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

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
    public async Task CreateProduct_ReturnsGrossPrice()
    {
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 10.00m, vatRate: VatRate.Reduced);

        product.Price.Should().Be(10.00m);
        product.GrossPrice.Should().Be(10.90m);
        product.VatRate.Should().Be("Reduced");
        product.Active.Should().BeTrue();
    }

    [TestMethod]
    public async Task CreateProduct_SameSku_ReturnsConflict()
    {
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync("/products", new CreateProductRequest(product.Sku.ToLowerInvariant(), "Copy", 1m, VatRate.Standard), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task GetProduct_Unknown_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri("/products/NOT-THERE", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task GetProduct_BeforeCreate_ThenCreated_ReturnsProduct()
    {
        var sku = "LATE-" + Guid.CreateVersion7().ToString("N")[12..];
        (await s_client.GetAsync(new Uri($"/products/{sku}", UriKind.Relative), TestContext.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await s_client.PostAsJsonAsync("/products", new CreateProductRequest(sku, "Late product", 3m, VatRate.Standard), TestContext.CancellationToken);

        var product = await s_client.GetFromJsonAsync<ProductResponse>($"/products/{sku}", TestContext.CancellationToken);
        product!.Name.Should().Be("Late product");
    }

    [TestMethod]
    public async Task GetProduct_LowerCaseSku_ReturnsProduct()
    {
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken);

        var product = await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku.ToLowerInvariant()}", TestContext.CancellationToken);

        product!.Id.Should().Be(created.Id);
    }

    [TestMethod]
    public async Task ChangePrice_ThenGet_ReturnsNewPrice()
    {
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 8.00m);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Price.Should().Be(8.00m);

        var response = await s_client.PutAsJsonAsync($"/products/{created.Sku}/price", new ChangePriceRequest(9.50m), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Price.Should().Be(9.50m);
    }

    [TestMethod]
    public async Task ChangePrice_Unknown_ReturnsNotFound()
    {
        var response = await s_client.PutAsJsonAsync("/products/NOT-THERE/price", new ChangePriceRequest(1m), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task Deactivate_ThenGet_ReturnsInactive()
    {
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Active.Should().BeTrue();

        var response = await s_client.DeleteAsync(new Uri($"/products/{created.Sku}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Active.Should().BeFalse();
    }
}
