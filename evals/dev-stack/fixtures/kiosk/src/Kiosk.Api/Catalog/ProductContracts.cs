namespace Kiosk.Api.Catalog;

public sealed record ProductDto(int Id, string Name, string Description, decimal Price, string? ImageUrl, string Category, string Sku, int Stock)
{
    public static ProductDto From(Product p) => new(p.Id, p.Name, p.Description, p.Price, p.ImageUrl, p.Category, p.Sku, p.Stock);
}

public sealed record UpdateProductRequest(string Name, string Description, decimal Price, string? ImageUrl, int Stock);

public sealed class ProductSearchRow
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
}
