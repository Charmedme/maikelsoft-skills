using Orders.Api.Catalog;

namespace Orders.Api.Tests;

internal sealed class FakeProductCatalog : IProductCatalog
{
    private static readonly Dictionary<string, Product> s_products = new(StringComparer.Ordinal)
    {
        ["SKU-1"] = new Product("SKU-1", "Coffee", 1m),
        ["SKU-2"] = new Product("SKU-2", "Cups", 1m),
        ["SKU-3"] = new Product("SKU-3", "Jug", 1m),
        ["SKU-OLD"] = Inactive(new Product("SKU-OLD", "Old filters", 1m)),
    };

    private static Product Inactive(Product product)
    {
        product.Deactivate();
        return product;
    }

    public Task<Product?> FindAsync(string sku, CancellationToken ct) =>
        Task.FromResult(s_products.GetValueOrDefault(Product.Normalize(sku)));

    public Task<bool> IsActiveAsync(string sku, CancellationToken ct) =>
        Task.FromResult(s_products.TryGetValue(Product.Normalize(sku), out var product) && product.IsActive);
}
