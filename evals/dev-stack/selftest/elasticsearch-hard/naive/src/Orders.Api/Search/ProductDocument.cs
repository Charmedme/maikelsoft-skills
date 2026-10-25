using Orders.Api.Catalog;
using Orders.Api.Inventory;

namespace Orders.Api.Search;

// The shape of a product in the search index. Property names are camelCase in the index.
public sealed record ProductDocument(
    string Id,
    string Sku,
    string Name,
    string Brand,
    string Category,
    decimal Price,
    bool Active,
    bool InStock,
    string? DescriptionNl,
    string? DescriptionDe,
    long Version)
{
    public static ProductDocument From(Product product, StockItem? stock) =>
        new(
            product.Id.ToString(),
            product.Sku,
            product.Name,
            product.Brand,
            product.Category,
            product.Price,
            product.Active,
            stock is { Available: > 0 },
            product.DescriptionNl,
            product.DescriptionDe,
            product.Version);
}
