using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Orders.Api.Tests.EvalProbes;

// Hidden tests of SHOP-59, the small story of the set.
[TestClass]
[DoNotParallelize]
public sealed class OrderSummaryProbe
{
    public TestContext TestContext { get; set; } = null!;

    private async Task<(OrdersApiFactory Factory, HttpClient Client)> StartAsync()
    {
        var factory = new OrdersApiFactory();
        await factory.MigrateAsync(TestContext.CancellationToken);
        return (factory, factory.CreateClient());
    }

    [TestMethod]
    public async Task E1_Summary_CountsLinesUnitsAndTotal()
    {
        var (factory, client) = await StartAsync();
        await using (factory)
        {
            var created = await client.PostAsJsonAsync("/orders", new { customerId = Guid.CreateVersion7(), lines = new object[] { new { sku = "A1", quantity = 2, unitPrice = 3m }, new { sku = "B1", quantity = 1, unitPrice = 4m } } }, TestContext.CancellationToken);
            var id = JsonDocument.Parse(await created.Content.ReadAsStringAsync(TestContext.CancellationToken)).RootElement.GetProperty("id").GetGuid();
            var body = JsonDocument.Parse(await client.GetStringAsync(new Uri($"/orders/{id}/summary", UriKind.Relative), TestContext.CancellationToken)).RootElement;
            Assert.AreEqual(2, body.GetProperty("lineCount").GetInt32());
            Assert.AreEqual(3, body.GetProperty("unitCount").GetInt32());
            Assert.AreEqual(10m, body.GetProperty("total").GetDecimal());
        }
    }

    [TestMethod]
    public async Task E2_Unknown_Returns404()
    {
        var (factory, client) = await StartAsync();
        await using (factory)
        {
            var response = await client.GetAsync(new Uri($"/orders/{Guid.CreateVersion7()}/summary", UriKind.Relative), TestContext.CancellationToken);
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
