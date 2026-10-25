using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Catalog;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PromotionEndpointsTests
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
    public async Task StartPromotion_GetShowsPromotionPrice()
    {
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 10.00m);

        var response = await s_client.PutAsJsonAsync($"/products/{created.Sku}/promotion", new StartPromotionRequest(20, s_factory.Time.GetUtcNow().AddDays(1)), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken);
        product!.Price.Should().Be(8.00m);
        product.ListPrice.Should().Be(10.00m);
        product.Promotion!.PercentOff.Should().Be(20);
    }

    [TestMethod]
    public async Task StartPromotion_EndInThePast_ReturnsValidationProblem()
    {
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken);

        var response = await s_client.PutAsJsonAsync($"/products/{created.Sku}/promotion", new StartPromotionRequest(20, s_factory.Time.GetUtcNow().AddMinutes(-1)), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task EndPromotion_GetShowsListPrice_OrderPaysListPrice()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var created = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 10.00m);
        await s_client.PutAsJsonAsync($"/products/{created.Sku}/promotion", new StartPromotionRequest(50, s_factory.Time.GetUtcNow().AddDays(1)), TestContext.CancellationToken);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Price.Should().Be(5.00m);

        var response = await s_client.DeleteAsync(new Uri($"/products/{created.Sku}/promotion", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await s_client.GetFromJsonAsync<ProductResponse>($"/products/{created.Sku}", TestContext.CancellationToken))!.Promotion.Should().BeNull();
        var order = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(created.Sku, 1)]), TestContext.CancellationToken);
        (await order.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!.Total.Should().Be(10.00m);
    }
}
