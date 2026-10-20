using Testcontainers.RabbitMq;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderEventTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task BrokerStarts()
    {
        await using var broker = new RabbitMqBuilder().Build();
        await broker.StartAsync(TestContext.CancellationToken);
        await Task.Delay(3000, TestContext.CancellationToken);
        Assert.IsFalse(string.IsNullOrEmpty(broker.GetConnectionString()));
    }
}
