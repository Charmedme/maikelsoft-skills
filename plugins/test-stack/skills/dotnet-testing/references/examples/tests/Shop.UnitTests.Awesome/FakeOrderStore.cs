namespace Shop.UnitTests.Awesome;

// A hand-written fake: simpler than a mock when the test needs real store behavior.
internal sealed class FakeOrderStore : IOrderStore
{
    private readonly Dictionary<int, Order> _orders = [];

    public IReadOnlyCollection<Order> Saved => _orders.Values;

    public void Add(Order order) => _orders[order.Id] = order;

    public Task<Order?> FindAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult(_orders.GetValueOrDefault(id));

    public Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
