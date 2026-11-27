namespace Kiosk.Api.Wishlists;

public sealed record CreateWishlistRequest(string Name, bool IsPublic);

public sealed record AddWishlistItemRequest(int ProductId, int Quantity, string? Note, decimal? TargetPrice);

public sealed record WishlistItemDto(int Id, int ProductId, string ProductName, decimal Price, int Quantity, string? Note, decimal? TargetPrice);

public sealed record WishlistDto(int Id, string Name, bool IsPublic, bool PriceAlerts, IReadOnlyList<WishlistItemDto> Items)
{
    public static WishlistDto From(Wishlist list) => new(
        list.Id,
        list.Name,
        list.IsPublic,
        list.PriceAlerts,
        list.Items.OrderBy(i => i.Id)
            .Select(i => new WishlistItemDto(i.Id, i.ProductId, i.Product?.Name ?? string.Empty, i.Product?.Price ?? 0m, i.Quantity, i.Note, i.TargetPrice))
            .ToList());
}

public sealed record WishlistSummary(int Id, string Name, int Items);

public sealed record ShareLinkResponse(string Url);
