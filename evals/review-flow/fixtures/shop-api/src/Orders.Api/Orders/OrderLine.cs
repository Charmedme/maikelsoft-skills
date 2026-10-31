namespace Orders.Api.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
        Sku = string.Empty;
    }

    public OrderLine(string sku, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public int Id { get; private set; }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;
}
