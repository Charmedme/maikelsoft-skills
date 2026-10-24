namespace Orders.Api.Messaging.Contracts;

// Consumed from warehouse.events. The warehouse service (.NET) sends it when goods arrive at the dock.
public sealed record StockReceivedV1(Guid ReceiptId, string Sku, int Quantity, DateTimeOffset ReceivedAt)
{
    public const string WireName = "stock.received.v1";
}
