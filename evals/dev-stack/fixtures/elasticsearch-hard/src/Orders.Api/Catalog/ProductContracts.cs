using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Catalog;

public sealed record CreateProductRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Range(typeof(decimal), "0", "100000")] decimal Price,
    VatRate VatRate,
    [property: Required, StringLength(80, MinimumLength = 1)] string Brand,
    [property: Required, RegularExpression("^[a-z0-9-]{1,40}$")] string Category);

public sealed record ChangePriceRequest(
    [property: Range(typeof(decimal), "0", "100000")] decimal Price);

public sealed record ProductResponse(Guid Id, string Sku, string Name, string Brand, string Category, decimal Price, decimal GrossPrice, string VatRate, bool Active);

public static class ProductMapping
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Id, product.Sku, product.Name, product.Brand, product.Category, product.Price, PriceCalculator.Gross(product.Price, product.VatRate), product.VatRate.ToString(), product.Active);
}
