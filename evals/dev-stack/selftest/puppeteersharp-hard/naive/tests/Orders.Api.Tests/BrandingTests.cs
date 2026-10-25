using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class BrandingTests
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
        s_client.Timeout = TimeSpan.FromSeconds(120);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_client.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task SetBranding_BusinessCustomer_ReturnsBranding()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);

        var response = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { logoUrl = (string?)null, footerText = "Goods-in: dock 4" }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var branding = await response.Content.ReadFromJsonAsync<BrandingResponse>(TestContext.CancellationToken);
        branding!.FooterText.Should().Be("Goods-in: dock 4");
    }

    [TestMethod]
    public async Task SetBranding_Consumer_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);

        var response = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { footerText = "Hello" }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SetBranding_HttpLogo_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);

        var response = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { logoUrl = "http://example.com/logo.png" }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task SetBranding_FooterTooLong_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken, CustomerKind.Business);

        var response = await s_client.PutAsJsonAsync($"/customers/{customer.Id}/branding", new { footerText = new string('x', 201) }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
