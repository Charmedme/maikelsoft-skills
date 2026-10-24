namespace Orders.Api.Catalog;

// The products that the first migration inserts. The product team maintains the real catalog in the admin tool.
public static class ProductSeed
{
    public static IReadOnlyList<object> Rows { get; } =
    [
        new { Sku = "SKU-1", Name = "Coffee beans 1 kg", UnitPrice = 18.50m, IsActive = true },
        new { Sku = "SKU-2", Name = "Espresso cups (6)", UnitPrice = 24.00m, IsActive = true },
        new { Sku = "SKU-3", Name = "Milk jug 600 ml", UnitPrice = 12.95m, IsActive = true },
        new { Sku = "SKU-4", Name = "Descaler", UnitPrice = 7.25m, IsActive = true },
        new { Sku = "SKU-5", Name = "Grinder burr set", UnitPrice = 39.00m, IsActive = true },
        new { Sku = "SKU-OLD", Name = "Paper filters (old size)", UnitPrice = 2.10m, IsActive = false },
    ];
}
