using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Api.Coupons;
using Orders.Api.Customers;
using Orders.Api.Jobs;
using Orders.Api.Orders;
using Orders.Api.Reports;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ExpressAndHistoryTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_client = null!;

    public TestContext TestContext { get; set; } = null!;

    private CancellationToken Ct => TestContext.CancellationToken;

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
    public async Task NextPick_ExpressBeforeOlderNormalOrder()
    {
        while ((await s_client.NextPickAsync("drain", Ct)).StatusCode == HttpStatusCode.OK)
        {
        }

        var normal = await s_client.CreateOrderAsync(Ct);
        s_factory.Time.Advance(TimeSpan.FromMinutes(1));
        var express = await PlaceAsync(Guid.CreateVersion7(), express: true);

        var first = await s_client.NextPickAsync("w-1", Ct);
        var second = await s_client.NextPickAsync("w-2", Ct);

        (await first.Content.ReadFromJsonAsync<OrderResponse>(Ct))!.Id.Should().Be(express.Id);
        (await second.Content.ReadFromJsonAsync<OrderResponse>(Ct))!.Id.Should().Be(normal.Id);
    }

    [TestMethod]
    public async Task PickingTimeout_PutsOrderBackInQueue()
    {
        while ((await s_client.NextPickAsync("drain", Ct)).StatusCode == HttpStatusCode.OK)
        {
        }

        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.NextPickAsync("w-slow", Ct);
        s_factory.Time.Advance(TimeSpan.FromMinutes(121));

        var released = await s_factory.Services.GetServices<IHostedService>().OfType<PickingTimeoutJob>().Single().RunOnceAsync(Ct);
        var after = await s_client.GetFromJsonAsync<OrderResponse>($"/orders/{order.Id}", Ct);

        released.Should().BeGreaterThanOrEqualTo(1);
        after!.Status.Should().Be("Placed");
    }

    [TestMethod]
    public async Task Coupon_SecondUseBySameCustomer_ReturnsValidationProblem()
    {
        await s_client.PostAsJsonAsync("/coupons", new CreateCouponRequest("WELCOME", 10m, 100, s_factory.Time.GetUtcNow().AddDays(30)), Ct);
        var customerId = Guid.CreateVersion7();
        await s_client.CreateOrderAsync(Ct, customerId, couponCode: "WELCOME");

        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 1, 1m)], "WELCOME"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task OrderHistory_SortedBySubtotal()
    {
        var customerId = Guid.CreateVersion7();
        var small = await s_client.CreateOrderAsync(Ct, customerId, unitPrice: 1m);
        var large = await s_client.CreateOrderAsync(Ct, customerId, unitPrice: 50m);

        var rows = await s_client.GetFromJsonAsync<List<OrderSummaryResponse>>($"/customers/{customerId}/orders?sort=Subtotal", Ct);

        rows!.Select(r => r.Id).Should().Equal(large.Id, small.Id);
    }

    [TestMethod]
    public async Task MonthlySales_SumsOrdersPerMonth()
    {
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero));
        await s_client.CreateOrderAsync(Ct, quantity: 2, unitPrice: 5m);

        var rows = await s_client.GetFromJsonAsync<List<MonthlySalesRow>>("/reports/monthly-sales?from=2026-11-01&to=2026-11-30", Ct);

        rows.Should().Equal(new MonthlySalesRow(new DateOnly(2026, 11, 1), 1, 10m));
    }

    private async Task<OrderResponse> PlaceAsync(Guid customerId, bool express)
    {
        var response = await s_client.PostAsJsonAsync("/orders", new CreateOrderRequest(customerId, [new CreateOrderLineRequest("SKU-1", 1, 1m)], null, express), Ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Ct))!;
    }
}
