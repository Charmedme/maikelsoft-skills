using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Orders;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OrderCacheTests
{
    private static OrdersApiFactory s_nodeA = null!;
    private static OrdersApiFactory s_nodeB = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static async Task ClassInitializeAsync(TestContext context)
    {
        s_nodeA = new OrdersApiFactory();
        s_nodeB = new OrdersApiFactory();
        await s_nodeA.MigrateAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        await s_nodeA.DisposeAsync();
        await s_nodeB.DisposeAsync();
    }

    [TestMethod]
    public async Task GetById_ReadTwice_ReturnsSameOrderWithLines()
    {
        using var client = s_nodeA.CreateClient();
        var created = await CreateAsync(client);

        var first = await client.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken);
        var second = await client.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken);

        second.Should().BeEquivalentTo(first);
        second!.Id.Should().Be(created.Id);
        second.Lines.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task Cancel_OnNodeA_NodeBReturnsCancelled()
    {
        using var clientA = s_nodeA.CreateClient();
        using var clientB = s_nodeB.CreateClient();
        var created = await CreateAsync(clientA);
        (await clientB.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken))!
            .Status.Should().Be("Placed");

        var cancel = await clientA.PostAsync(new Uri($"/orders/{created.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        cancel.StatusCode.Should().Be(HttpStatusCode.OK);
        (await clientB.GetFromJsonAsync<OrderResponse>($"/orders/{created.Id}", TestContext.CancellationToken))!
            .Status.Should().Be("Cancelled");
    }

    [TestMethod]
    public async Task Cancel_UnknownOrder_ReturnsNotFound()
    {
        using var client = s_nodeA.CreateClient();

        var response = await client.PostAsync(new Uri($"/orders/{Guid.CreateVersion7()}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<OrderResponse> CreateAsync(HttpClient client)
    {
        var request = new CreateOrderRequest(
            Guid.CreateVersion7(),
            [new CreateOrderLineRequest("SKU-1", 1, 2m), new CreateOrderLineRequest("SKU-2", 2, 3m)]);
        var response = await client.PostAsJsonAsync("/orders", request, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(TestContext.CancellationToken))!;
    }
}
