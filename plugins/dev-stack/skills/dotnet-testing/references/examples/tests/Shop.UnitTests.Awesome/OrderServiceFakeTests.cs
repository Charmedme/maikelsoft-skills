using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Shop.UnitTests.Awesome;

[TestClass]
public sealed class OrderServiceFakeTests
{
    private readonly FakeOrderStore _store = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CloseAsync_OpenOrder_StoresClosedOrder()
    {
        _store.Add(new Order(7, 10m, null, []));
        var sut = new OrderService(_store, _time);

        await sut.CloseAsync(7, TestContext.CancellationToken);

        _store.Saved.Should().ContainSingle().Which.ClosedAt.Should().Be(_time.GetUtcNow());
    }
}
