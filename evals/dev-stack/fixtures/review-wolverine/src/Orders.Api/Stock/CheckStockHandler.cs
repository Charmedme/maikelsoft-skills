using Orders.Api.Contracts;

namespace Orders.Api.Stock;

public static class CheckStockHandler
{
    private static readonly HashSet<string> s_outOfStock = new(StringComparer.OrdinalIgnoreCase) { "SKU-0" };

    public static bool Handle(CheckStock message) =>
        !s_outOfStock.Contains(message.Sku) && message.Quantity <= 100;
}
