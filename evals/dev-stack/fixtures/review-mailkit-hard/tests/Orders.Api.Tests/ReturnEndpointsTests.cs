using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orders.Api.Data;
using Orders.Api.Returns;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ReturnEndpointsTests
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
    public async Task CreateReturn_NotShipped_ReturnsValidationProblem()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/orders/{order.Id}/returns", new CreateReturnRequest(ReturnReason.NoLongerNeeded, null), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [TestMethod]
    public async Task CreateReturn_AfterDispatch_ReturnsCreated()
    {
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        var order = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken);
        var shipment = await s_client.CreateShipmentAsync(order.Id, TestContext.CancellationToken);
        await s_client.PostAsJsonAsync($"/shipments/{shipment.Id}/dispatch", new DispatchShipmentRequest("dpd", "DPD0001"), TestContext.CancellationToken);

        var response = await s_client.PostAsJsonAsync($"/orders/{order.Id}/returns", new CreateReturnRequest(ReturnReason.Damaged, "The box was wet."), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<ReturnResponse>(TestContext.CancellationToken))!.Status.Should().Be("Requested");
        await using var scope = s_factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        (await db.OutboxMails.AnyAsync(m => m.ToAddress == customer.Email && m.Subject == "We received your return request", TestContext.CancellationToken)).Should().BeTrue();
    }
}
