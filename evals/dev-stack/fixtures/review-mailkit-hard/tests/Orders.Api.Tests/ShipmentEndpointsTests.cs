using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Mail;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ShipmentEndpointsTests
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
    public async Task Dispatch_KnownCarrier_ShipsOrderAndReturnsTrackingUrl()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl", "3SABCD1234567"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dispatched = await response.Content.ReadFromJsonAsync<ShipmentResponse>(TestContext.CancellationToken);
        dispatched!.Status.Should().Be("Dispatched");
        dispatched.TrackingUrl.Should().Be(new Uri("https://jouw.postnl.nl/track-and-trace/3SABCD1234567"));
        var reloaded = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", TestContext.CancellationToken);
        reloaded!.Status.Should().Be("Shipped");
    }

    [TestMethod]
    public async Task Dispatch_UnknownCarrier_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("pigeon", "12345"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Dispatch_Twice_ReturnsConflict()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dhl", "JD0001"), TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dhl", "JD0002"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task CreateShipment_SecondForSameOrder_ReturnsConflict()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync("/shipments", new CreateShipmentRequest(order.Id), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task Dispatch_AddsShippingNoticeToOutbox()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dpd", "DPD0002"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var notice = await db.OutboxMails.SingleAsync(m => m.ToAddress == customer.Email && m.Subject == ShippingNoticeMail.Subject, TestContext.CancellationToken);
        notice.HtmlBody.Should().Contain("DPD0002");
    }
}
