namespace Orders.Api.Search;

public sealed record ProductSearchHit(string Sku, string Name, string Brand, string Category, decimal Price);

public sealed record ProductSearchResponse(IReadOnlyList<ProductSearchHit> Items, long Total, int Page, int PageSize);
