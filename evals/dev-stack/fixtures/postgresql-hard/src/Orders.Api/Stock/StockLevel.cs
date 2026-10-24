namespace Orders.Api.Stock;

public sealed class StockLevel
{
    private StockLevel()
    {
        Sku = string.Empty;
    }

    public StockLevel(string sku, int available, DateTimeOffset updatedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(available);

        Sku = sku;
        Available = available;
        UpdatedAt = updatedAt;
    }

    public string Sku { get; private set; }

    public int Available { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Set(int available, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(available);
        Available = available;
        UpdatedAt = now;
    }
}
