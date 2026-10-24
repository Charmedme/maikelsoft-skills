using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Stock;

public sealed record SetStockRequest([property: Range(0, 1_000_000)] int Available);

public sealed record StockResponse(string Sku, int Available, DateTimeOffset UpdatedAt);

public static class StockMapping
{
    public static StockResponse ToResponse(this StockLevel level) => new(level.Sku, level.Available, level.UpdatedAt);
}
