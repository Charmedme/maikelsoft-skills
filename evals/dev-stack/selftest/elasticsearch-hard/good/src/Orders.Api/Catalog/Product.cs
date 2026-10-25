namespace Orders.Api.Catalog;

public sealed class Product
{
    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
        Brand = string.Empty;
        Category = string.Empty;
    }

    public Product(string sku, string name, decimal price, VatRate vatRate, string brand, string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(brand);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Brand = brand.Trim();
        Category = category.Trim().ToLowerInvariant();
        Price = price;
        VatRate = vatRate;
        Active = true;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public string Brand { get; private set; }

    public string Category { get; private set; }

    public decimal Price { get; private set; }

    public VatRate VatRate { get; private set; }

    public bool Active { get; private set; }

    public string? DescriptionNl { get; private set; }

    public string? DescriptionDe { get; private set; }

    // Increased by OrdersDbContext on each save. The search index uses it as the external version.
    public long Version { get; private set; }

    public void ChangePrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Price = price;
    }

    public void Deactivate() => Active = false;

    // A null or empty value removes the description in that language.
    public void Describe(string? dutch, string? german)
    {
        DescriptionNl = string.IsNullOrWhiteSpace(dutch) ? null : dutch.Trim();
        DescriptionDe = string.IsNullOrWhiteSpace(german) ? null : german.Trim();
    }
}

public enum VatRate
{
    Standard = 0,
    Reduced = 1,
    Zero = 2,
}
