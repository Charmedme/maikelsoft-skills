namespace Orders.Api.Catalog;

public sealed class Product
{
    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Product(string sku, string name, decimal price, VatRate vatRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Price = price;
        VatRate = vatRate;
        Active = true;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal Price { get; private set; }

    public VatRate VatRate { get; private set; }

    public bool Active { get; private set; }

    public int? PromotionPercentOff { get; private set; }

    public DateTimeOffset? PromotionEndsAt { get; private set; }

    public void ChangePrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Price = price;
    }

    public void Deactivate() => Active = false;

    public void StartPromotion(int percentOff, DateTimeOffset endsAt)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(percentOff, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentOff, 90);
        PromotionPercentOff = percentOff;
        PromotionEndsAt = endsAt;
    }

    public void EndPromotion()
    {
        PromotionPercentOff = null;
        PromotionEndsAt = null;
    }

    public bool HasPromotionAt(DateTimeOffset now) => PromotionPercentOff is not null && PromotionEndsAt > now;

    public decimal PriceAt(DateTimeOffset now) => HasPromotionAt(now) ? Promotions.Discounted(Price, PromotionPercentOff!.Value) : Price;
}

public enum VatRate
{
    Standard = 0,
    Reduced = 1,
    Zero = 2,
}
