using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orders.Api.Jobs;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ReturnEndpointsTests
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
    public async Task CreateReturn_ShippedOrderInWindow_ReturnsCreated()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);

        var response = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(order.Id, "Wrong size"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<ReturnResponse>(Ct))!.Status.Should().Be("Requested");
    }

    [TestMethod]
    public async Task CreateReturn_OrderNotShipped_ReturnsConflict()
    {
        var order = await s_client.CreateOrderAsync(Ct);

        var response = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(order.Id, "Changed my mind"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task CreateReturn_AfterWindow_ReturnsConflict()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);
        s_factory.Time.Advance(TimeSpan.FromDays(31));

        var response = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(order.Id, "Too late"), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [TestMethod]
    public async Task ExpiryJob_OldRequest_IsExpired()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);
        var created = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(order.Id, "Broken"), Ct);
        var request = (await created.Content.ReadFromJsonAsync<ReturnResponse>(Ct))!;
        s_factory.Time.Advance(TimeSpan.FromDays(15));

        var job = s_factory.Services.GetServices<IHostedService>().OfType<ReturnExpiryJob>().Single();
        await job.RunOnceAsync(Ct);

        var after = await s_client.GetFromJsonAsync<ReturnResponse>($"/returns/{request.Id}", Ct);
        after!.Status.Should().Be("Expired");
    }

    [TestMethod]
    public async Task MarkReceived_ExpiredRequest_ReturnsConflict()
    {
        var order = await s_client.CreateOrderAsync(Ct);
        await s_client.ShipAsync(order.Id, Ct);
        var created = await s_client.PostAsJsonAsync("/returns", new CreateReturnRequest(order.Id, "Broken"), Ct);
        var request = (await created.Content.ReadFromJsonAsync<ReturnResponse>(Ct))!;
        s_factory.Time.Advance(TimeSpan.FromDays(15));
        await s_factory.Services.GetServices<IHostedService>().OfType<ReturnExpiryJob>().Single().RunOnceAsync(Ct);

        var response = await s_client.PostAsync(new Uri($"/returns/{request.Id}/received", UriKind.Relative), null, Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
