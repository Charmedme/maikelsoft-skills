using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Catalog;

public sealed record CreateProductRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Range(typeof(decimal), "0", "100000")] decimal Price,
    VatRate VatRate);

public sealed record ChangePriceRequest(
    [property: Range(typeof(decimal), "0", "100000")] decimal Price);

public sealed record StartPromotionRequest(
    [property: Range(1, 90)] int PercentOff,
    [property: Required] DateTimeOffset? EndsAt);

public sealed record PromotionResponse(int PercentOff, DateTimeOffset EndsAt);

public sealed record ProductResponse(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    decimal GrossPrice,
    string VatRate,
    bool Active,
    decimal ListPrice,
    PromotionResponse? Promotion);

public static class ProductMapping
{
    public static ProductResponse ToResponse(this Product product, DateTimeOffset now)
    {
        var price = product.PriceAt(now);
        var promotion = product.HasPromotionAt(now) ? new PromotionResponse(product.PromotionPercentOff!.Value, product.PromotionEndsAt!.Value) : null;
        return new(product.Id, product.Sku, product.Name, price, PriceCalculator.Gross(price, product.VatRate), product.VatRate.ToString(), product.Active, product.Price, promotion);
    }
}
