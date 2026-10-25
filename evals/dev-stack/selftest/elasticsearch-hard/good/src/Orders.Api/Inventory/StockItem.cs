namespace Orders.Api.Inventory;

public sealed class StockItem
{
    private StockItem()
    {
        Sku = string.Empty;
    }

    public StockItem(string sku)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        Sku = sku.Trim().ToUpperInvariant();
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public int OnHand { get; private set; }

    public int Reserved { get; private set; }

    public int Available => OnHand - Reserved;

    public uint Version { get; private set; }

    // Increased by OrdersDbContext on each save, for the external version of the search index.
    // Version above is xmin: 32 bits, it wraps around, so it cannot be an external version.
    public long SearchVersion { get; private set; }

    public void Receive(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        OnHand += quantity;
    }

    public bool TryReserve(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (quantity > Available)
        {
            return false;
        }

        Reserved += quantity;
        return true;
    }

    public void Release(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Reserved = Math.Max(0, Reserved - quantity);
    }

    public void Ship(int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        if (quantity > Reserved)
        {
            throw new InvalidOperationException("Cannot ship more than the reserved quantity.");
        }

        Reserved -= quantity;
        OnHand -= quantity;
    }
}
