using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Products;

public sealed record CreateProductRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Range(typeof(decimal), "0", "100000")] decimal Price);

public sealed record ChangePriceRequest([property: Range(typeof(decimal), "0", "100000")] decimal Price);

public sealed record ProductResponse(string Sku, string Name, decimal Price, bool Active);

public static class ProductMapping
{
    public static ProductResponse ToResponse(this Product product) => new(product.Sku, product.Name, product.Price, product.Active);
}
