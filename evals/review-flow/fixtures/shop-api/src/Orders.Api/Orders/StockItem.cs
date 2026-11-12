namespace Orders.Api.Orders;

public sealed class StockItem
{
    private StockItem()
    {
        Sku = string.Empty;
    }

    public StockItem(string sku, int available)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(available);
        Sku = sku;
        Available = available;
    }

    public string Sku { get; private set; }

    public int Available { get; private set; }

    public bool TryReserve(int quantity)
    {
        if (quantity > Available)
        {
            return false;
        }

        Available -= quantity;
        return true;
    }

    public void Release(int quantity) => Available += quantity;
}
