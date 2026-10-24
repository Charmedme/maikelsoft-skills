using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class PickingEndpointsTests
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
    public async Task NextPick_OldestPlacedOrderFirst()
    {
        await DrainAsync();
        var first = await s_client.CreateOrderAsync(Ct);
        s_factory.Time.Advance(TimeSpan.FromMinutes(1));
        var second = await s_client.CreateOrderAsync(Ct);

        var a = await PickAsync("w-1");
        var b = await PickAsync("w-2");

        a.Id.Should().Be(first.Id);
        b.Id.Should().Be(second.Id);
        a.Status.Should().Be("Picking");
    }

    [TestMethod]
    public async Task NextPick_NoPlacedOrder_ReturnsNoContent()
    {
        await DrainAsync();

        var response = await s_client.NextPickAsync("w-1", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [TestMethod]
    public async Task NextPick_NoWorkerId_ReturnsValidationProblem()
    {
        var response = await s_client.PostAsync(new Uri("/picking/next", UriKind.Relative), null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task NextPick_CancelledOrder_IsSkipped()
    {
        await DrainAsync();
        var cancelled = await s_client.CreateOrderAsync(Ct);
        await s_client.PostAsync(new Uri($"/orders/{cancelled.Id}/cancel", UriKind.Relative), null, Ct);
        var placed = await s_client.CreateOrderAsync(Ct);

        var picked = await PickAsync("w-3");

        picked.Id.Should().Be(placed.Id);
    }

    private async Task<OrderResponse> PickAsync(string worker)
    {
        var response = await s_client.NextPickAsync(worker, Ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(Ct))!;
    }

    private async Task DrainAsync()
    {
        while ((await s_client.NextPickAsync("drain", Ct)).StatusCode == HttpStatusCode.OK)
        {
        }
    }
}
