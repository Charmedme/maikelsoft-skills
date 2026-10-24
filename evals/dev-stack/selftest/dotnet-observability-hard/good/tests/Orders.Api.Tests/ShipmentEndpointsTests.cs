using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
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

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dispatched = await response.Content.ReadFromJsonAsync<ShipmentResponse>(TestContext.CancellationToken);
        dispatched!.Status.Should().Be("Dispatched");
        dispatched.TrackingNumber.Should().StartWith("POSTNL");
        dispatched.TrackingUrl!.ToString().Should().StartWith("https://jouw.postnl.nl/track-and-trace/POSTNL");
        s_factory.Carriers.Requests.Should().Contain(r => r.ShipmentId == shipment.Id && r.Address.PostalCode == "1234 AB");
        var reloaded = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", TestContext.CancellationToken);
        reloaded!.Status.Should().Be("Shipped");
    }

    [TestMethod]
    public async Task Dispatch_Refused_ReturnsConflictWithReason_AndStaysPending()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, postalCode: "0000 XX");
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Should().Contain("POSTAL_CODE_INVALID");
        (await s_client.GetFromJsonAsync<ShipmentResponse>($"/shipments/{shipment.Id}", TestContext.CancellationToken))!.Status.Should().Be("Pending");
    }

    [TestMethod]
    public async Task Dispatch_GatewayFails_ReturnsBadGateway_AndStaysPending()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, postalCode: "9999 XX");
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await s_client.GetFromJsonAsync<ShipmentResponse>($"/shipments/{shipment.Id}", TestContext.CancellationToken))!.Status.Should().Be("Pending");
    }

    [TestMethod]
    public async Task Dispatch_UnknownCarrier_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("pigeon"), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task Dispatch_Twice_ReturnsConflict()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dhl"), TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dhl"), TestContext.CancellationToken);

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
}
