using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Reporting;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SalesReportTests
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
    public async Task Sales_OneDay_SumsKeptOrders()
    {
        // The report reads all orders of the day. Each test class has its own customers, so read the delta.
        var before = await s_client.GetFromJsonAsync<SalesReport>("/reports/sales?from=2026-10-16&to=2026-10-16", TestContext.CancellationToken);
        var customer = await s_client.CreateCustomerAsync(TestContext.CancellationToken);
        await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, unitPrice: 10m);
        var cancelled = await s_client.CreateOrderAsync(customer.Id, TestContext.CancellationToken, unitPrice: 99m);
        await s_client.PostAsync(new Uri($"/orders/{cancelled.Id}/cancel", UriKind.Relative), null, TestContext.CancellationToken);

        var report = await s_client.GetFromJsonAsync<SalesReport>("/reports/sales?from=2026-10-16&to=2026-10-16", TestContext.CancellationToken);

        report!.Days.Should().ContainSingle();
        report.Days[0].Orders.Should().Be(before!.Days[0].Orders + 2);
        report.Days[0].CancelledOrders.Should().Be(before.Days[0].CancelledOrders + 1);
        report.TotalNetRevenue.Should().Be(before.TotalNetRevenue + 20m);
    }

    [TestMethod]
    public async Task Sales_RangeTooLong_ReturnsValidationProblem()
    {
        var response = await s_client.GetAsync(new Uri("/reports/sales?from=2026-01-01&to=2026-12-31", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
