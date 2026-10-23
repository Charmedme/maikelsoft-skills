namespace Orders.Api.Catalog;

public sealed class Product
{
    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Product(string sku, string name, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        Sku = Normalize(sku);
        Name = name;
        UnitPrice = unitPrice;
        IsActive = true;
    }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal UnitPrice { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;

    public static string Normalize(string sku) => sku.Trim().ToUpperInvariant();
}
