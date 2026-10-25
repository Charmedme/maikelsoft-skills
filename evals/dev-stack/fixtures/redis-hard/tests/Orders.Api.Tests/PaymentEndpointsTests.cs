using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Payments;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PaymentEndpointsTests
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
    public async Task Pay_Authorized_ReturnsCreated()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/payments", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(TestContext.CancellationToken);
        payment!.Status.Should().Be("Authorized");
        payment.Amount.Should().Be(5.00m);
    }

    [TestMethod]
    public async Task Pay_Declined_ReturnsUnprocessableWithReason()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, unitPrice: 600m);

        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/payments", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var payment = await response.Content.ReadFromJsonAsync<PaymentResponse>(TestContext.CancellationToken);
        payment!.DeclineReason.Should().Be("insufficient_funds");
    }

    [TestMethod]
    public async Task Pay_Twice_ReturnsConflict()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        await s_client.PostAsync(new Uri($"/orders/{order.Id}/payments", UriKind.Relative), null, TestContext.CancellationToken);

        var response = await s_client.PostAsync(new Uri($"/orders/{order.Id}/payments", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
