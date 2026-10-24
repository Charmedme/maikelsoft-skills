namespace Orders.Api.Products;

/// <summary>A customer wants a mail when a SKU is in stock again. The mail job reads the alerts with a NotifiedAt value.</summary>
public sealed class BackInStockAlert
{
    private BackInStockAlert()
    {
        Sku = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; }

    public Guid CustomerId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTime? NotifiedAt { get; private set; }
}
