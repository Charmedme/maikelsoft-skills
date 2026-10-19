namespace Orders.Api.Products;

public sealed class Product
{
    private Product()
    {
        Sku = string.Empty;
    }

    public Product(string sku, decimal price, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Sku = sku;
        Price = price;
        UpdatedAt = now;
    }

    public string Sku { get; private set; }

    public decimal Price { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void ChangePrice(decimal price, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);

        Price = price;
        UpdatedAt = now;
    }
}
