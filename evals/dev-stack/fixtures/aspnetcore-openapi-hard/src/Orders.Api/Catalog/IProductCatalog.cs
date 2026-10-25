namespace Orders.Api.Catalog;

public interface IProductCatalog
{
    Task<Product?> FindAsync(string sku, CancellationToken ct);

    Task<bool> IsActiveAsync(string sku, CancellationToken ct);
}
