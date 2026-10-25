using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JasperFx.Core;
using Orders.Api.Contracts;
using Orders.Api.Shipping;
using Wolverine;
using Wolverine.Tracking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class BookShipmentEndpointTests
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
    public async Task Book_PendingShipment_StoresTrackingAndPublishesShipmentBooked()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        HttpResponseMessage? response = null;

        var session = await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ => response = await s_client.PostAsJsonAsync(
                $"/shipments/{shipment.Id}/book", new BookShipmentRequest(ShippingSpeed.Express), TestContext.CancellationToken)));

        response!.StatusCode.Should().Be(HttpStatusCode.OK);
        var booked = await response.Content.ReadFromJsonAsync<ShipmentResponse>(TestContext.CancellationToken);
        booked!.Status.Should().Be("Booked");
        booked.Carrier.Should().Be("dhl");
        session.Sent.SingleMessage<ShipmentBooked>().TrackingNumber.Should().Be(booked.TrackingNumber);
        s_factory.Carrier.Requests.Should().Contain(r => r.Reference == shipment.Id.ToString("N") && r.Speed == ShippingSpeed.Express);
    }

    [TestMethod]
    public async Task Book_AddressRejected_ReturnsUnprocessable()
    {
        s_factory.Carrier.RejectedPostalCodes["9999 ZZ"] = "Unknown postal code.";
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, address: ApiClient.Address("9999 ZZ"));
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/book", new BookShipmentRequest(ShippingSpeed.Standard), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var current = await s_client.GetFromJsonAsync<ShipmentResponse>($"/shipments/{shipment.Id}", TestContext.CancellationToken);
        current!.Status.Should().Be("Pending");
    }

    [TestMethod]
    public async Task Book_BookedShipment_ReturnsConflict()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/book", new BookShipmentRequest(ShippingSpeed.Standard), TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/book", new BookShipmentRequest(ShippingSpeed.Standard), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
