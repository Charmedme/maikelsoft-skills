namespace Orders.Api.Search;

// A product that changed and must be indexed again. OrdersDbContext writes it in the same transaction as the change.
public sealed class ProductSearchOutboxEntry
{
    private ProductSearchOutboxEntry()
    {
    }

    public ProductSearchOutboxEntry(Guid productId, DateTimeOffset createdAt)
    {
        ProductId = productId;
        CreatedAt = createdAt;
    }

    public long Id { get; private set; }

    public Guid ProductId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
}
