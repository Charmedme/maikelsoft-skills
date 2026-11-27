using Kiosk.Api.Catalog;
using Kiosk.Api.Users;

namespace Kiosk.Api.Wishlists;

public sealed class Wishlist
{
    public int Id { get; set; }

    public int OwnerId { get; set; }

    public User? Owner { get; set; }

    public required string Name { get; set; }

    public bool IsPublic { get; set; }

    /// <summary>The token of the share link. Null until the owner shares the list.</summary>
    public string? ShareToken { get; set; }

    public bool PriceAlerts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<WishlistItem> Items { get; set; } = [];
}

public sealed class WishlistItem
{
    public int Id { get; set; }

    public int WishlistId { get; set; }

    public int ProductId { get; set; }

    public Product? Product { get; set; }

    public int Quantity { get; set; } = 1;

    public string? Note { get; set; }

    /// <summary>The customer gets a price alert when the price drops to this amount or lower.</summary>
    public decimal? TargetPrice { get; set; }
}
