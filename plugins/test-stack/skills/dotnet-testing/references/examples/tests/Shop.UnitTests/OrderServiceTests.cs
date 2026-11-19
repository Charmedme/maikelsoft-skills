using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Shop.UnitTests;

[TestClass]
public sealed class OrderServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);
    private readonly IOrderStore _store = Substitute.For<IOrderStore>();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly OrderService _sut;

    public OrderServiceTests() => _sut = new OrderService(_store, _time);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task CloseAsync_OpenOrder_SetsClosedAtToNow()
    {
        _store.FindAsync(7, Arg.Any<CancellationToken>()).Returns(new Order(7, 10m, null, ["a", "b"]));

        Order result = await _sut.CloseAsync(7, TestContext.CancellationToken);

        result.Should().BeEquivalentTo(new Order(7, 10m, Now, ["a", "b"]));
    }

    [TestMethod]
    public async Task CloseAsync_OpenOrder_SavesClosedOrder()
    {
        _store.FindAsync(7, Arg.Any<CancellationToken>()).Returns(new Order(7, 10m, null, []));

        await _sut.CloseAsync(7, TestContext.CancellationToken);

        await _store.Received(1).SaveAsync(Arg.Is<Order>(o => o.ClosedAt == Now), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task CloseAsync_ClosedOrder_ThrowsInvalidOperation()
    {
        _store.FindAsync(7, Arg.Any<CancellationToken>()).Returns(new Order(7, 10m, Now.AddDays(-1), []));

        Func<Task> act = () => _sut.CloseAsync(7, TestContext.CancellationToken);

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("*already closed*");
    }

    [TestMethod]
    public async Task CloseAsync_UnknownOrder_ThrowsKeyNotFound()
    {
        Func<Task> act = () => _sut.CloseAsync(99, TestContext.CancellationToken);

        await act.Should().ThrowExactlyAsync<KeyNotFoundException>();
    }
}
