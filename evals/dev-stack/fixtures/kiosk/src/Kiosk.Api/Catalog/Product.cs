namespace Kiosk.Api.Catalog;

public sealed class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public string Category { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string SupplierEmail { get; set; } = string.Empty;

    public int Stock { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
