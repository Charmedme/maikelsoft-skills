using FluentAssertions;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Shipping;
using Wolverine.Tracking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ParcelBookingTests
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
    public async Task PaymentCaptured_BooksParcelAndSendsPickOrder()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);

        var session = await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .SendMessageAndWaitAsync(Capture(order.Id, ShippingSpeed.Express));

        var shipment = await ShipmentAsync(order.Id);
        shipment.Status.Should().Be(ShipmentStatus.Booked);
        var pick = session.Sent.SingleMessage<PickOrder>();
        pick.ShipmentId.Should().Be(shipment.Id);
        pick.TrackingNumber.Should().Be(shipment.TrackingNumber);
        s_factory.Carrier.Requests.Should().ContainSingle(r => r.Reference.StartsWith(shipment.Id.ToString("N"), StringComparison.Ordinal)).Which.Speed.Should().Be(ShippingSpeed.Express);
    }

    [TestMethod]
    public async Task PaymentCaptured_Twice_BooksOneParcel()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var capture = Capture(order.Id, ShippingSpeed.Standard);

        await s_factory.WolverineHost.TrackActivity().Timeout(30.Seconds()).SendMessageAndWaitAsync(capture);
        var second = await s_factory.WolverineHost.TrackActivity().Timeout(30.Seconds()).SendMessageAndWaitAsync(capture);

        second.Sent.MessagesOf<PickOrder>().Should().BeEmpty();
        var shipment = await ShipmentAsync(order.Id);
        s_factory.Carrier.Requests.Count(r => r.Reference.StartsWith(shipment.Id.ToString("N"), StringComparison.Ordinal)).Should().Be(1);
    }

    [TestMethod]
    public async Task PaymentCaptured_AddressRejected_MarksAddressProblemOnce()
    {
        s_factory.Carrier.RejectedPostalCodes["9999 ZY"] = "Unknown postal code.";
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, address: ApiClient.Address("9999 ZY"));

        var session = await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .SendMessageAndWaitAsync(Capture(order.Id, ShippingSpeed.Standard));

        var shipment = await ShipmentAsync(order.Id);
        shipment.Status.Should().Be(ShipmentStatus.AddressProblem);
        shipment.ProblemReason.Should().Be("Unknown postal code.");
        session.Sent.MessagesOf<PickOrder>().Should().BeEmpty();
        s_factory.Carrier.Requests.Count(r => r.Reference.StartsWith(shipment.Id.ToString("N"), StringComparison.Ordinal)).Should().Be(1);
    }

    [TestMethod]
    public async Task BookParcel_PlatformDown_IsRetried()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, address: ApiClient.Address("7271 XX"));
        s_factory.Carrier.FailingPostalCodes.Add("7271 XX");
        await s_factory.WolverineHost.TrackActivity().Timeout(30.Seconds()).DoNotAssertOnExceptionsDetected()
            .InvokeMessageAndWaitAsync(Capture(order.Id, ShippingSpeed.Standard));
        var shipment = await ShipmentAsync(order.Id);

        // The retry policy for HttpRequestException in Program.cs handles this exception.
        var act = () => s_factory.WolverineHost.TrackActivity().Timeout(30.Seconds())
            .InvokeMessageAndWaitAsync(new BookParcel(shipment.Id, ShippingSpeed.Standard));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static PaymentCapturedV2 Capture(Guid orderId, ShippingSpeed speed) =>
        new(Guid.CreateVersion7(), orderId, 5m, "EUR", s_factory.Time.GetUtcNow(), speed);

    private static async Task<Shipment> ShipmentAsync(Guid orderId)
    {
        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.Shipments.AsNoTracking().SingleAsync(s => s.OrderId == orderId);
    }
}
