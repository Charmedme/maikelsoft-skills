namespace Orders.Api.Stock;

public sealed class StockLevel
{
    public StockLevel(string sku, int available)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(available);

        Sku = sku;
        Available = available;
    }

    public string Sku { get; private set; }

    public int Available { get; private set; }

    public void Replace(int available)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(available);
        Available = available;
    }

    public bool TryReserve(int quantity)
    {
        if (quantity > Available)
        {
            return false;
        }

        Available -= quantity;
        return true;
    }
}
