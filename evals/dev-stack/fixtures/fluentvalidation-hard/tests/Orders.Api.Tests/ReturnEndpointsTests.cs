using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ReturnEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;
    private static HttpClient s_admin = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_client = s_factory.CreateClient();
        s_admin = s_factory.CreateAdminClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        s_admin.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task CreateReturn_ShippedOrder_ReturnsCreated()
    {
        var orderId = await ShippedOrderAsync(("SKU-1", 2));

        using var response = await s_client.PostAsJsonAsync(
            "/returns", new CreateReturnRequest(orderId, "sku-1", 1, ReturnReason.Damaged, "Broken bag"), TestContext.CancellationToken);
        var created = await response.Content.ReadFromJsonAsync<ReturnResponse>(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        created!.Sku.Should().Be("SKU-1");
        created.Status.Should().Be(ReturnStatus.Requested);
    }

    [TestMethod]
    public async Task CreateReturn_MoreThanOrdered_Returns422()
    {
        var orderId = await ShippedOrderAsync(("SKU-2", 1));
        using var first = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(orderId, "SKU-2", 1, ReturnReason.WrongItem, null), TestContext.CancellationToken);

        using var second = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(orderId, "SKU-2", 1, ReturnReason.WrongItem, null), TestContext.CancellationToken);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [TestMethod]
    public async Task CreateReturn_UnknownOrder_Returns404()
    {
        using var response = await s_client.PostAsJsonAsync(
            "/returns", new CreateReturnRequest(Guid.CreateVersion7(), "SKU-1", 1, ReturnReason.Damaged, null), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task CreateReturn_MissingReason_Returns400()
    {
        using var response = await s_client.PostAsJsonAsync(
            "/returns", new { orderId = Guid.CreateVersion7(), sku = "SKU-1", quantity = 1 }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    [TestMethod]
    public async Task GetReturn_Unknown_Returns404()
    {
        using var response = await s_client.GetAsync(new Uri($"/returns/{Guid.CreateVersion7()}", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> ShippedOrderAsync(params (string Sku, int Quantity)[] lines)
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(null, lines), TestContext.CancellationToken);
        using var shipped = await s_admin.PostAsJsonAsync($"/admin/orders/{order.Id}/ship", new { carrier = "DHL", trackingCode = "JVGL0001" }, TestContext.CancellationToken);
        shipped.StatusCode.Should().Be(HttpStatusCode.OK);
        return order.Id;
    }
}
