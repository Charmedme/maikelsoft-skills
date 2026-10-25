using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Reports;
using Orders.Api.Tracking;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class TrackingAndPickingTests
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
    public async Task Track_KnownCode_ReturnsStatusAndShipments()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);

        var tracking = await s_client.GetFromJsonAsync<TrackingResponse>($"/track/{order.TrackingCode}", Ct);

        order.TrackingCode.Should().HaveLength(TrackingCodes.Length);
        tracking!.Status.Should().Be("Shipped");
        tracking.Shipments.Should().ContainSingle().Which.Carrier.Should().Be("PostNL");
    }

    [TestMethod]
    public async Task Track_UnknownCode_ReturnsNotFound()
    {
        var response = await s_client.GetAsync(new Uri("/track/AAAAAAAAAAAA", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [TestMethod]
    public async Task NextPick_ParallelWorkers_EachOrderOnce()
    {
        while ((await s_client.NextPickAsync("drain", Ct)).StatusCode == HttpStatusCode.OK)
        {
        }

        for (var i = 0; i < 8; i++)
        {
            await s_client.CreateOrderAsync(Ct);
        }

        var picked = new ConcurrentBag<Guid>();
        await Task.WhenAll(Enumerable.Range(0, 16).Select(async worker =>
        {
            var response = await s_client.NextPickAsync($"w-{worker}", Ct);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                picked.Add((await response.Content.ReadFromJsonAsync<OrderResponse>(Ct))!.Id);
            }
        }));

        picked.Should().HaveCount(8).And.OnlyHaveUniqueItems();
    }

    [TestMethod]
    public async Task WeeklySales_SundayLateEvening_CountsInThatDutchWeek()
    {
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 22, 22, 30, 0, TimeSpan.Zero)); // Sunday 23:30 in Amsterdam
        await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 6m);
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 22, 23, 30, 0, TimeSpan.Zero)); // Monday 00:30 in Amsterdam
        await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 7m);

        var rows = await s_client.GetFromJsonAsync<List<WeeklySalesRow>>("/reports/weekly-sales?from=2026-11-16&to=2026-11-29", Ct);

        rows.Should().Equal(
            new WeeklySalesRow(new DateOnly(2026, 11, 16), 1, 6m),
            new WeeklySalesRow(new DateOnly(2026, 11, 23), 1, 7m));
    }
}
