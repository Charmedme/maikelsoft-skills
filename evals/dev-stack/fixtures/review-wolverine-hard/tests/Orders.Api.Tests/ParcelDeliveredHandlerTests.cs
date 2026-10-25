using System.Net.Http.Json;
using FluentAssertions;
using JasperFx.Core;
using Orders.Api.Contracts;
using Orders.Api.Shipping;
using Wolverine.Tracking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ParcelDeliveredHandlerTests
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
    public async Task ParcelDelivered_DispatchedShipment_MarksDelivered()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        var tracking = $"3S{Guid.CreateVersion7():N}"[..20].ToUpperInvariant();
        await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("postnl", tracking), TestContext.CancellationToken);

        await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .InvokeMessageAndWaitAsync(new ParcelDelivered("postnl", tracking, s_factory.Time.GetUtcNow().AddDays(1)));

        var current = await s_client.GetFromJsonAsync<ShipmentResponse>($"/shipments/{shipment.Id}", TestContext.CancellationToken);
        current!.Status.Should().Be("Delivered");
    }

    [TestMethod]
    public async Task ParcelDelivered_UnknownParcel_IsIgnored()
    {
        var act = () => s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .InvokeMessageAndWaitAsync(new ParcelDelivered("dpd", "UNKNOWN-1", s_factory.Time.GetUtcNow()));

        await act.Should().NotThrowAsync();
    }
}
