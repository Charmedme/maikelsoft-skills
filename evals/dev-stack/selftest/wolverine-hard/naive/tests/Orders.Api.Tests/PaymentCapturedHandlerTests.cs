using FluentAssertions;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Orders;
using Wolverine.Tracking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PaymentCapturedHandlerTests
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
    public async Task PaymentCaptured_PlacedOrder_MarksPaidAndPublishesOrderPaid()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var capturedAt = s_factory.Time.GetUtcNow().AddMinutes(3);

        var session = await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .InvokeMessageAndWaitAsync(new PaymentCapturedV2(Guid.CreateVersion7(), order.Id, 5.00m, "EUR", capturedAt, ShippingSpeed.Standard));

        session.Sent.SingleMessage<OrderPaid>().OrderId.Should().Be(order.Id);
        (await StatusAsync(order.Id)).Should().Be(OrderStatus.Paid);
    }

    [TestMethod]
    public async Task PaymentCaptured_UnknownOrder_SendsNothing()
    {
        var session = await s_factory.WolverineHost.TrackActivity()
            .Timeout(30.Seconds())
            .InvokeMessageAndWaitAsync(new PaymentCapturedV2(Guid.CreateVersion7(), Guid.CreateVersion7(), 5.00m, "EUR", s_factory.Time.GetUtcNow(), ShippingSpeed.Standard));

        session.Sent.MessagesOf<OrderPaid>().Should().BeEmpty();
    }

    private static async Task<OrderStatus> StatusAsync(Guid orderId)
    {
        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        return await db.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => o.Status).SingleAsync();
    }
}
