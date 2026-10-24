using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Invoices;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class InvoiceEndpointsTests
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
    public async Task CreateInvoice_ShippedOrders_GetConsecutiveNumbers()
    {
        var first = await ShippedOrderAsync();
        var second = await ShippedOrderAsync();

        var a = await InvoiceAsync(first);
        var b = await InvoiceAsync(second);

        a.Number.Should().StartWith("2026-");
        long.Parse(b.Number[5..], System.Globalization.CultureInfo.InvariantCulture)
            .Should().Be(long.Parse(a.Number[5..], System.Globalization.CultureInfo.InvariantCulture) + 1);
    }

    [TestMethod]
    public async Task CreateInvoice_Twice_ReturnsConflictWithoutGap()
    {
        var order = await ShippedOrderAsync();
        var first = await InvoiceAsync(order);

        var again = await s_client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(order), Ct);
        var next = await InvoiceAsync(await ShippedOrderAsync());

        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        long.Parse(next.Number[5..], System.Globalization.CultureInfo.InvariantCulture)
            .Should().Be(long.Parse(first.Number[5..], System.Globalization.CultureInfo.InvariantCulture) + 1);
    }

    [TestMethod]
    public async Task CreateInvoice_OrderNotShipped_ReturnsConflict()
    {
        var order = await s_client.CreateOrderAsync(Ct);

        var response = await s_client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(order.Id), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task<Guid> ShippedOrderAsync()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);
        return order.Id;
    }

    private async Task<InvoiceResponse> InvoiceAsync(Guid orderId)
    {
        var response = await s_client.PostAsJsonAsync("/invoices", new CreateInvoiceRequest(orderId), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<InvoiceResponse>(Ct))!;
    }
}
