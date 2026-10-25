using Orders.Api.Catalog;

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
    long Version)
{
    public static ProductDocument From(Product product) =>
        new(product.Id.ToString(), product.Sku, product.Name, product.Brand, product.Category, product.Price, product.Active, product.Version);
}
