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
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_client = s_factory.CreateClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task ListProducts_ReturnsOnlyActiveProducts()
    {
        var products = await s_client.GetFromJsonAsync<List<ProductResponse>>("/products", TestContext.CancellationToken);

        products.Should().Contain(p => p.Sku == "SKU-1");
        products.Should().NotContain(p => p.Sku == "SKU-OLD");
    }

    [TestMethod]
    public async Task GetProduct_LowerCaseSku_FindsProduct()
    {
        var product = await s_client.GetFromJsonAsync<ProductResponse>("/products/sku-2", TestContext.CancellationToken);

        product!.Sku.Should().Be("SKU-2");
        product.UnitPrice.Should().Be(24.00m);
    }

    [TestMethod]
    public async Task GetProduct_Inactive_IsStillFound()
    {
        var product = await s_client.GetFromJsonAsync<ProductResponse>("/products/SKU-OLD", TestContext.CancellationToken);

        product!.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetProduct_Unknown_Returns404()
    {
        using var response = await s_client.GetAsync(new Uri("/products/NOPE", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
