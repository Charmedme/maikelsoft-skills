using System.ComponentModel;

namespace Orders.Api.Catalog;

/// <summary>
/// The cached form of a product. It holds the promotion and its end, not the price at the time of caching,
/// so the price is computed on each read and a promotion ends at its end time.
/// </summary>
[ImmutableObject(true)]
public sealed record ProductSnapshot(
    Guid Id,
    string Sku,
    string Name,
    decimal ListPrice,
    VatRate VatRate,
    bool Active,
    int? PromotionPercentOff,
    DateTimeOffset? PromotionEndsAt)
{
    public static ProductSnapshot From(Product product) =>
        new(product.Id, product.Sku, product.Name, product.Price, product.VatRate, product.Active, product.PromotionPercentOff, product.PromotionEndsAt);

    public bool HasPromotionAt(DateTimeOffset now) => PromotionPercentOff is not null && PromotionEndsAt > now;

    public decimal PriceAt(DateTimeOffset now) => HasPromotionAt(now) ? Promotions.Discounted(ListPrice, PromotionPercentOff!.Value) : ListPrice;

    public ProductResponse ToResponse(DateTimeOffset now)
    {
        var price = PriceAt(now);
        var promotion = HasPromotionAt(now) ? new PromotionResponse(PromotionPercentOff!.Value, PromotionEndsAt!.Value) : null;
        return new(Id, Sku, Name, price, PriceCalculator.Gross(price, VatRate), VatRate.ToString(), Active, ListPrice, promotion);
    }
}
