namespace Orders.Api.Orders;

public interface IProductCatalog
{
    Task<bool> ExistsAsync(string sku, CancellationToken ct);
}

// In-memory catalog until the product service exists.
public sealed class InMemoryProductCatalog : IProductCatalog
{
    private static readonly HashSet<string> s_skus = new(StringComparer.OrdinalIgnoreCase) { "SKU-1", "SKU-2", "SKU-3" };

    public async Task<bool> ExistsAsync(string sku, CancellationToken ct)
    {
        await Task.Yield();
        return s_skus.Contains(sku);
    }
}
