using Kiosk.Api.Basket;
using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Wishlists;

/// <summary>Share links for wish lists: friends open the list without an account and buy from it.</summary>
public static class WishlistSharingEndpoints
{
    public static IEndpointRouteBuilder MapWishlistSharingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/wishlists/{id:int}/share", ShareAsync).RequireAuthorization();
        app.MapGet("/wishlists/shared/{token}", ViewSharedAsync)
            .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(60)).Tag("wishlists"));
        app.MapPost("/wishlists/shared/{token}/buy", BuyAsync).RequireAuthorization(Policies.WebSession);
        app.MapGet("/wishlists/shared/{token}/back", (string? returnUrl) => Results.LocalRedirect(returnUrl ?? "/"));
        app.MapPost("/api/wishlists/{id:int}/alerts/unsubscribe", UnsubscribeAsync);
        return app;
    }

    private static async Task<IResult> ShareAsync(int id, HttpContext http, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var list = await db.Wishlists.SingleOrDefaultAsync(w => w.Id == id && w.OwnerId == userId, ct);
        if (list is null)
        {
            return Results.NotFound();
        }

        list.ShareToken ??= WishlistTokens.NewShareToken();
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync("wishlists", ct);
        return Results.Ok(new ShareLinkResponse($"{http.Request.Scheme}://{http.Request.Host}/wishlists/shared/{list.ShareToken}"));
    }

    private static async Task<IResult> ViewSharedAsync(string token, KioskDbContext db, CancellationToken ct)
    {
        var list = await db.Wishlists.AsNoTracking()
            .Include(w => w.Owner)
            .Include(w => w.Items).ThenInclude(i => i.Product)
            .SingleOrDefaultAsync(w => w.ShareToken == token, ct);
        return list is null ? Results.NotFound() : Results.Ok(list);
    }

    // Puts the items of a shared list into the basket of the visitor and returns to the page of the shop.
    private static async Task<IResult> BuyAsync(string token, [FromForm] string? returnUrl, HttpContext http, KioskDbContext db, BasketService baskets, IOutputCacheStore cache, CancellationToken ct)
    {
        var list = await db.Wishlists.Include(w => w.Items).SingleOrDefaultAsync(w => w.ShareToken == token, ct);
        if (list is null)
        {
            return Results.NotFound();
        }

        var basket = await baskets.ForUserAsync(http.User.GetUserId(), ct);
        foreach (var item in list.Items)
        {
            basket.Items.Add(new BasketItem { ProductId = item.ProductId, Quantity = item.Quantity });
        }

        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(BasketEndpoints.CacheTag, ct);
        var target = string.IsNullOrEmpty(returnUrl) ? "/basket" : returnUrl;
        return target.StartsWith('/') ? Results.Redirect(target) : Results.Redirect("/basket");
    }

    private static async Task<IResult> UnsubscribeAsync(int id, string token, KioskDbContext db, CancellationToken ct)
    {
        if (token != WishlistTokens.AlertToken(id))
        {
            return Results.BadRequest();
        }

        await db.Wishlists.Where(w => w.Id == id).ExecuteUpdateAsync(s => s.SetProperty(w => w.PriceAlerts, false), ct);
        return Results.NoContent();
    }
}
