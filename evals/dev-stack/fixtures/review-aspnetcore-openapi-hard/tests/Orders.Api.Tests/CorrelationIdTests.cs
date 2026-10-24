using FluentAssertions;

namespace Orders.Api.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CorrelationIdTests
{
    private static OrdersApiFactory s_factory = null!;

    public TestContext TestContext { get; set; } = null!;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context) => s_factory = new OrdersApiFactory();

    [ClassCleanup]
    public static async Task ClassCleanupAsync() => await s_factory.DisposeAsync();

    [TestMethod]
    public async Task Response_EchoesCorrelationId()
    {
        using var client = s_factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "abc-123");

        using var response = await client.GetAsync(new Uri("/healthz/live", UriKind.Relative), TestContext.CancellationToken);

        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("abc-123");
    }

    [TestMethod]
    public async Task HealthReady_DatabaseUp_ReturnsHealthy()
    {
        using var client = s_factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/healthz/ready", UriKind.Relative), TestContext.CancellationToken);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
