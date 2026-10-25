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
            .InvokeMessageAndWaitAsync(new PaymentCapturedV2(Guid.CreateVersion7(), order.Id, 5m, "EUR", s_factory.Time.GetUtcNow(), ShippingSpeed.Express));

        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var shipment = await db.Shipments.AsNoTracking().SingleAsync(s => s.OrderId == order.Id, TestContext.CancellationToken);
        shipment.Status.Should().Be(ShipmentStatus.Booked);
        session.Sent.SingleMessage<PickOrder>().ShipmentId.Should().Be(shipment.Id);
    }
}
