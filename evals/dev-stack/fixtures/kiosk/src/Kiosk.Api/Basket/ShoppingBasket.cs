namespace Kiosk.Api.Basket;

public sealed class ShoppingBasket
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? CouponCode { get; set; }

    public List<BasketItem> Items { get; set; } = [];
}
