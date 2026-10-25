using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Reports;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class SalesReportTests
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
    public async Task DailySales_SumsLinesOfOrdersPerDay()
    {
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 2, 10, 0, 0, TimeSpan.Zero));
        await s_client.CreateOrderAsync(Ct, quantity: 2, unitPrice: 2.50m);
        await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 10m);
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 3, 12, 0, 0, TimeSpan.Zero));
        await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 4m);

        var rows = await s_client.GetFromJsonAsync<List<DailySalesRow>>("/reports/daily-sales?from=2026-11-02&to=2026-11-03", Ct);

        rows.Should().Equal(
            new DailySalesRow(new DateOnly(2026, 11, 2), 2, 15m),
            new DailySalesRow(new DateOnly(2026, 11, 3), 1, 4m));
    }

    [TestMethod]
    public async Task DailySales_CancelledOrder_IsNotCounted()
    {
        s_factory.Time.SetUtcNow(new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero));
        await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 3m);
        var cancelled = await s_client.CreateOrderAsync(Ct, quantity: 1, unitPrice: 100m);
        await s_client.PostAsync(new Uri($"/orders/{cancelled.Id}/cancel", UriKind.Relative), null, Ct);

        var rows = await s_client.GetFromJsonAsync<List<DailySalesRow>>("/reports/daily-sales?from=2026-11-09&to=2026-11-09", Ct);

        rows.Should().Equal(new DailySalesRow(new DateOnly(2026, 11, 9), 1, 3m));
    }

    [TestMethod]
    public async Task DailySales_RangeTooLong_ReturnsValidationProblem()
    {
        var response = await s_client.GetAsync(new Uri("/reports/daily-sales?from=2025-01-01&to=2026-06-30", UriKind.Relative), Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
