using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Admin;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AdminEndpointsTests
{
    private static OrdersApiFactory s_factory = null!;
    private static HttpClient s_admin = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        s_factory = new OrdersApiFactory();
        s_admin = s_factory.CreateAdminClient();
    }

    [ClassCleanup]
    public static async Task ClassCleanupAsync()
    {
        s_admin.Dispose();
        await s_factory.DisposeAsync();
    }

    [TestMethod]
    public async Task Stats_WithKey_ReturnsCounts()
    {
        var stats = await s_admin.GetFromJsonAsync<AdminStatsResponse>("/admin/stats", TestContext.CancellationToken);

        stats!.ActiveProducts.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public async Task Stats_WithoutKey_Returns401()
    {
        using var client = s_factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/admin/stats", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [TestMethod]
    public async Task Deactivate_UnknownProduct_Returns404()
    {
        using var response = await s_admin.PostAsync(new Uri("/admin/products/NOPE/deactivate", UriKind.Relative), null, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
