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

    public void ChangePrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Price = price;
    }

    public void Deactivate() => Active = false;
}

public enum VatRate
{
    Standard = 0,
    Reduced = 1,
    Zero = 2,
}
