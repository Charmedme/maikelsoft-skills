namespace Orders.Api.Products;

public sealed class Product
{
    private Product()
    {
        Sku = string.Empty;
        Name = string.Empty;
    }

    public Product(string sku, string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Sku = sku;
        Name = name;
        Price = price;
        Active = true;
    }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public decimal Price { get; private set; }

    public bool Active { get; private set; }

    public void ChangePrice(decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        Price = price;
    }

    public void Deactivate() => Active = false;
}
