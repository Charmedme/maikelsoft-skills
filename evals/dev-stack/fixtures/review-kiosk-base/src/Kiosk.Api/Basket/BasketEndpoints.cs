using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Orders;
using Kiosk.Api.Users;
using Microsoft.AspNetCore.OutputCaching;

namespace Kiosk.Api.Basket;

public static class BasketEndpoints
{
    public const string CacheTag = "baskets";

    public static IEndpointRouteBuilder MapBasketEndpoints(this IEndpointRouteBuilder app)
    {
        var baskets = app.MapGroup("/api/baskets").RequireAuthorization();
        baskets.MapGet("/mine", GetMineAsync).CacheOutput(OutputCacheSetup.BasketSnapshot);
        baskets.MapGet("/{id:int}", GetAsync);
        baskets.MapPost("/mine/items", AddItemAsync);
        baskets.MapDelete("/mine/items/{itemId:int}", RemoveItemAsync);
        baskets.MapPost("/mine/checkout", CheckoutAsync);
        return app;
    }

    private static async Task<BasketDto> GetMineAsync(HttpContext http, BasketService baskets, CancellationToken ct) =>
        BasketService.ToDto(await baskets.ForUserAsync(http.User.GetUserId(), ct));

    private static async Task<IResult> GetAsync(int id, HttpContext http, BasketService baskets, CancellationToken ct) =>
        await baskets.FindAsync(id, ct) is { } basket && (basket.UserId == http.User.GetUserId() || http.User.IsInRole(Roles.Admin))
            ? Results.Ok(BasketService.ToDto(basket))
            : Results.NotFound();

    private static async Task<IResult> AddItemAsync(HttpContext http, AddItemRequest request, BasketService baskets, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        if (request.Quantity is < 1 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Enter a quantity between 1 and 100."] });
        }

        if (await db.Products.FindAsync([request.ProductId], ct) is null)
        {
            return Results.NotFound();
        }

        var basket = await baskets.ForUserAsync(http.User.GetUserId(), ct);
        var existing = basket.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (existing is null)
        {
            basket.Items.Add(new BasketItem { ProductId = request.ProductId, Quantity = request.Quantity });
        }
        else
        {
            existing.Quantity += request.Quantity;
        }

        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(CacheTag, ct);
        return Results.Ok(BasketService.ToDto(await baskets.ForUserAsync(http.User.GetUserId(), ct)));
    }

    private static async Task<IResult> RemoveItemAsync(HttpContext http, int itemId, BasketService baskets, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        var basket = await baskets.ForUserAsync(http.User.GetUserId(), ct);
        var item = basket.Items.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return Results.NotFound();
        }

        basket.Items.Remove(item);
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(CacheTag, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> CheckoutAsync(HttpContext http, CheckoutRequest request, BasketService baskets, OrderService orders, KioskDbContext db, IOutputCacheStore cache, CancellationToken ct)
    {
        var userId = http.User.GetUserId();
        var basket = await baskets.ForUserAsync(userId, ct);
        if (basket.Items.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["basket"] = ["The basket is empty."] });
        }

        var lines = basket.Items.Select(i => new OrderLineRequest(i.ProductId, i.Quantity)).ToList();
        var result = await orders.PlaceAsync(new PlaceOrder(userId, null, lines, request.DeliveryAddress, request.CouponCode ?? basket.CouponCode), ct);
        if (result.Order is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [result.Error ?? "The order failed."] });
        }

        basket.Items.Clear();
        basket.CouponCode = null;
        await db.SaveChangesAsync(ct);
        await cache.EvictByTagAsync(CacheTag, ct);
        return Results.Created($"/api/orders/{result.Order.Id}", OrderDto.From(result.Order));
    }
}