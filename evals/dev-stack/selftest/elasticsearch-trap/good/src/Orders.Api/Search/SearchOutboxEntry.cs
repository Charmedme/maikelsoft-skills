namespace Orders.Api.Search;

// One row for each change of an order. The worker indexes the current state of the order.
public sealed class SearchOutboxEntry
{
    private SearchOutboxEntry()
    {
    }

    public SearchOutboxEntry(Guid orderId)
    {
        OrderId = orderId;
    }

    public long Id { get; private set; }

    public Guid OrderId { get; private set; }
}
