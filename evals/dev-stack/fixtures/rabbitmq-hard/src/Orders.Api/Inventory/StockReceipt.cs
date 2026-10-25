namespace Orders.Api.Inventory;

// A goods receipt of the warehouse that is booked on the stock. The receipt id makes the booking idempotent.
public sealed class StockReceipt
{
    private StockReceipt()
    {
        Sku = string.Empty;
    }

    public StockReceipt(Guid receiptId, string sku, int quantity, DateTimeOffset receivedAt)
    {
        ReceiptId = receiptId;
        Sku = sku;
        Quantity = quantity;
        ReceivedAt = receivedAt;
    }

    public Guid ReceiptId { get; private set; }

    public string Sku { get; private set; }

    public int Quantity { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }
}
