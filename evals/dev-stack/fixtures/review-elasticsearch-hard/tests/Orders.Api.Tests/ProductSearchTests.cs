using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Search;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ProductSearchTests
{
    private static OrdersApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task InitializeAsync(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        await s_factory.MigrateAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task CleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Search_ByName_FindsNewProduct()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("NAME");
        await client.CreateProductAsync(sku, "Thermosbeker Zanzibar", ct);

        var result = await client.SearchUntilAsync("q=zanzibar", r => r.Items.Any(i => i.Sku == sku), ct);

        result.Items.Should().ContainSingle(i => i.Sku == sku).Which.Name.Should().Be("Thermosbeker Zanzibar");
    }

    [TestMethod]
    public async Task Search_WithTypo_FindsProduct()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("TYPO");
        await client.CreateProductAsync(sku, "Fluitketel Marrakesh", ct);

        var result = await client.SearchUntilAsync("q=marrakech", r => r.Items.Any(i => i.Sku == sku), ct);

        result.Items.Should().Contain(i => i.Sku == sku);
    }

    [TestMethod]
    public async Task Search_CategoryFilter_ReturnsOnlyThatCategory()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var kitchen = SearchTestHelpers.UniqueSku("CAT");
        var garden = SearchTestHelpers.UniqueSku("CAT");
        await client.CreateProductAsync(kitchen, "Schaal Valparaiso", ct, category: "kitchen");
        await client.CreateProductAsync(garden, "Schaal Valparaiso", ct, category: "garden");
        await client.SearchUntilAsync("q=valparaiso", r => r.Items.Count(i => i.Sku == kitchen || i.Sku == garden) == 2, ct);

        var result = await client.SearchUntilAsync("q=valparaiso&category=garden", r => r.Items.Any(i => i.Sku == garden), ct);

        result.Items.Should().Contain(i => i.Sku == garden).And.NotContain(i => i.Sku == kitchen);
    }

    [TestMethod]
    public async Task Search_DeactivatedProduct_IsNotShown()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("OFF");
        await client.CreateProductAsync(sku, "Vergiet Timboektoe", ct);
        await client.SearchUntilAsync("q=timboektoe", r => r.Items.Any(i => i.Sku == sku), ct);

        (await client.DeleteAsync($"/products/{sku}", ct)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var result = await client.SearchUntilAsync("q=timboektoe", r => r.Items.All(i => i.Sku != sku), ct);

        result.Items.Should().NotContain(i => i.Sku == sku);
    }

    [TestMethod]
    public async Task Search_PriceChange_IsIndexed()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("PRICE");
        await client.CreateProductAsync(sku, "Pepermolen Kathmandu", ct, price: 20m);
        await client.SearchUntilAsync("q=kathmandu", r => r.Items.Any(i => i.Sku == sku), ct);

        (await client.PutAsJsonAsync($"/products/{sku}/price", new { price = 17.5m }, ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await client.SearchUntilAsync("q=kathmandu", r => r.Items.Any(i => i.Sku == sku && i.Price == 17.5m), ct);

        result.Items.Should().ContainSingle(i => i.Sku == sku).Which.Price.Should().Be(17.5m);
    }

    [TestMethod]
    public async Task Search_PageZero_ReturnsValidationProblem()
    {
        using var client = s_factory.CreateClient();

        var response = await client.GetAsync("/products/search?q=x&page=0", TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Search_DutchDescription_FindsProduct()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("LANG");
        await client.CreateProductAsync(sku, "Model Oslo", ct);
        (await client.PutAsJsonAsync($"/products/{sku}/descriptions", new { nl = "Lampen voor de tuin", de = "Lampen für den Garten" }, ct)).StatusCode.Should().Be(HttpStatusCode.OK);

        var result = (await client.GetFromJsonAsync<ProductSearchResponse>("/products/search?q=lamp&lang=nl", ct))!;

        result.Items.Should().Contain(i => i.Sku == sku);
    }

    [TestMethod]
    public async Task Search_LastItemReserved_ProductIsNotShown()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("STOCK");
        await client.CreateProductAsync(sku, "Kruidenrek Lissabon", ct, stock: 1);
        await Task.Delay(TimeSpan.FromSeconds(2), ct); // the worker indexes within a second

        (await client.PostAsJsonAsync($"/inventory/{sku}/reservations", new { quantity = 1 }, ct)).StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        var result = (await client.GetFromJsonAsync<ProductSearchResponse>("/products/search?q=lissabon", ct))!;

        result.Items.Should().NotContain(i => i.Sku == sku);
    }

    [TestMethod]
    public async Task Search_BrandFilter_FindsBrand()
    {
        var ct = TestContext.CancellationToken;
        using var client = s_factory.CreateClient();
        var sku = SearchTestHelpers.UniqueSku("BRAND");
        await client.CreateProductAsync(sku, "Bidonhouder Bergen", ct, brand: "Van Moof");

        var result = await client.SearchUntilAsync("q=bergen&brand=Van%20Moof", r => r.Items.Any(i => i.Sku == sku), ct);

        result.Items.Should().ContainSingle(i => i.Sku == sku);
    }

    [TestMethod]
    public async Task Search_PageBeyondResultWindow_ReturnsValidationProblem()
    {
        using var client = s_factory.CreateClient();

        var response = await client.GetAsync("/products/search?q=x&page=501", TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
