using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Shop.IntegrationTests;

[TestClass]
[DoNotParallelize] // The tests share one database.
public sealed class OrdersEndpointTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public Task ResetDatabaseAsync() => TestHost.ResetDatabaseAsync(TestContext.CancellationToken);

    [TestMethod]
    public async Task GetCount_EmptyDatabase_ReturnsZero()
    {
        using HttpClient client = TestHost.Factory.CreateClient();

        long count = await client.GetFromJsonAsync<long>("/orders/count", TestContext.CancellationToken);

        count.Should().Be(0);
    }

    [TestMethod]
    public async Task Post_ValidOrder_ReturnsCreated()
    {
        using HttpClient client = TestHost.Factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync("/orders", new { Total = 10.5m }, TestContext.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
