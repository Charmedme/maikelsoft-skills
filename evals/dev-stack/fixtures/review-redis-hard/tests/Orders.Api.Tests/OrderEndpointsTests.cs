using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Catalog;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderEndpointsTests
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
    public async Task CreateOrder_ValidRequest_ReturnsCreatedOrder()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 2.50m);
        var request = new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(product.Sku, 3)], "PO-77", "Leave at the back door.");

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        order!.CustomerId.Should().Be(customer.Id);
        order.Total.Should().Be(7.50m);
        order.Status.Should().Be("Placed");
        order.CustomerReference.Should().Be("PO-77");
        order.DeliveryInstructions.Should().Be("Leave at the back door.");
        order.PlacedAt.Should().Be(s_factory.Time.GetUtcNow());
    }

    [TestMethod]
    public async Task CreateOrder_UnknownCustomer_ReturnsValidationProblem()
    {
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken);
        var request = new CreateOrderRequest(Guid.CreateVersion7(), [new CreateOrderLineRequest(product.Sku, 1)]);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateOrder_NoLines_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var request = new CreateOrderRequest(customer.Id, []);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [TestMethod]
    public async Task CreateOrder_UnknownProduct_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var request = new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest("NO-SUCH-SKU", 1)]);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateOrder_DeactivatedProduct_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken);
        await s_client.DeleteAsync(new Uri($"/products/{product.Sku}", UriKind.Relative), TestContext.CancellationToken);
        var request = new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(product.Sku, 1)]);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateOrder_UsesCatalogPrice()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var product = await s_client.CreateProductAsync(TestContext.CancellationToken, price: 4.00m);
        await s_client.PutAsJsonAsync($"/products/{product.Sku}/price", new ChangePriceRequest(5.00m), TestContext.CancellationToken);
        var request = new CreateOrderRequest(customer.Id, [new CreateOrderLineRequest(product.Sku, 2)]);

        var response = await s_client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken);
        order!.Lines.Should().ContainSingle().Which.UnitPrice.Should().Be(5.00m);
        order.Total.Should().Be(10.00m);
    }

    [TestMethod]
    public async Task ListOrders_ForCustomer_ReturnsOnlyTheirOrders()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var other = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        await s_client.CreateOrderAsync(other.Id, TestContext.CancellationToken);

        var orders = await s_client.GetFromJsonAsync<List<OrderResponse>>($"/orders?customerId={customer.Id}", TestContext.CancellationToken);

        orders.Should().ContainSingle().Which.CustomerId.Should().Be(customer.Id);
    }

    [TestMethod]
    public async Task CancelOrder_InsideWindow_ReturnsCancelledOrder()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!.Status.Should().Be("Cancelled");
    }

    [TestMethod]
    public async Task HealthReady_DatabaseUp_ReturnsHealthy()
    {
        var response = await s_client.GetAsync(new Uri("/healthz/ready", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
