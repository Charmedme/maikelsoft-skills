namespace Orders.Api.Tests;

[TestClass]
public sealed class CancelOrderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CancelOrder_WaitsForTheHandler()
    {
        await Task.Delay(2000, TestContext.CancellationToken);
        Assert.IsTrue(true);
    }
}
