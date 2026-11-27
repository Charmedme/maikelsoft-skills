using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Basket;

public sealed class BasketService(KioskDbContext db)
{
    public async Task<ShoppingBasket?> FindAsync(int basketId, CancellationToken ct) =>
        await db.Baskets.Include(b => b.Items).ThenInclude(i => i.Product).SingleOrDefaultAsync(b => b.Id == basketId, ct);

    public async Task<ShoppingBasket> ForUserAsync(int userId, CancellationToken ct)
    {
        var basket = await db.Baskets.Include(b => b.Items).ThenInclude(i => i.Product).SingleOrDefaultAsync(b => b.UserId == userId, ct);
        if (basket is null)
        {
            basket = new ShoppingBasket { UserId = userId };
            db.Baskets.Add(basket);
            await db.SaveChangesAsync(ct);
        }

        return basket;
    }

    public static BasketDto ToDto(ShoppingBasket basket)
    {
        var items = basket.Items
            .OrderBy(i => i.Id)
            .Select(i => new BasketItemDto(i.Id, i.ProductId, i.Product?.Name ?? string.Empty, i.Quantity, i.Product?.Price ?? 0m))
            .ToList();
        return new BasketDto(basket.Id, basket.CouponCode, items, items.Sum(i => i.UnitPrice * i.Quantity));
    }
}
