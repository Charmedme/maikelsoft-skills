namespace Orders.Api.Catalog;

public sealed record ProductResponse(string Sku, string Name, decimal UnitPrice, bool IsActive);

public static class ProductMapping
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Sku, product.Name, product.UnitPrice, product.IsActive);
}
