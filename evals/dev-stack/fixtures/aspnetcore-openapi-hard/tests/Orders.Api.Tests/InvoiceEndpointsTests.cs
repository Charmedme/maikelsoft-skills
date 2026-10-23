using System.Net;
using System.Text;
using FluentAssertions;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class InvoiceEndpointsTests
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
    public async Task GetInvoice_ExistingOrder_ReturnsPdf()
    {
        var order = await TestData.PlaceOrderAsync(s_client, TestData.Order(null, ("SKU-3", 2)), TestContext.CancellationToken);

        using var response = await s_client.GetAsync(new Uri($"/orders/{order.Id}/invoice", UriKind.Relative), TestContext.CancellationToken);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        Encoding.ASCII.GetString(bytes, 0, 8).Should().Be("%PDF-1.4");
    }

    [TestMethod]
    public async Task GetInvoice_UnknownOrder_Returns404Problem()
    {
        using var response = await s_client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}/invoice", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }
}
