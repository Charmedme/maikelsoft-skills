using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Inventory;

public sealed record ReceiveStockRequest([property: Range(1, 100000)] int Quantity);

public sealed record ReserveStockRequest([property: Range(1, 1000)] int Quantity);

public sealed record StockCountLine(
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Range(0, 1_000_000)] int OnHand);

public sealed record StockCountRequest([property: Required, MinLength(1), MaxLength(5000)] IReadOnlyList<StockCountLine> Lines);

public sealed record StockCountResponse(int Applied);

public sealed record StockResponse(string Sku, int OnHand, int Reserved, int Available);

public static class StockMapping
{
    public static StockResponse ToResponse(this StockItem item) => new(item.Sku, item.OnHand, item.Reserved, item.Available);
}
