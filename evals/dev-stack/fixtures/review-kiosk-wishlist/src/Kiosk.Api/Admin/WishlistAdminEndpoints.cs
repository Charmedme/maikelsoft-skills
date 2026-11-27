using Kiosk.Api.Data;
using Kiosk.Api.Hosting;
using Kiosk.Api.Wishlists;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Admin;

public sealed record PopularWish(int ProductId, string Name, int Lists);

public static class WishlistAdminEndpoints
{
    public static IEndpointRouteBuilder MapWishlistAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/admin/wishlists");
        admin.MapGet("/", (string email, WishlistQueries queries, CancellationToken ct) => queries.OfOwnerAsync(email, ct))
            .RequireAuthorization(Policies.Admin);
        admin.MapGet("/popular", PopularAsync).RequireAuthorization(Policies.Staff);
        admin.MapDelete("/{id:int}", DeleteAsync);
        return app;
    }

    private static async Task<List<PopularWish>> PopularAsync(KioskDbContext db, CancellationToken ct) =>
        await db.Set<WishlistItem>()
            .GroupBy(i => new { i.ProductId, i.Product!.Name })
            .Select(g => new PopularWish(g.Key.ProductId, g.Key.Name, g.Select(i => i.WishlistId).Distinct().Count()))
            .OrderByDescending(p => p.Lists)
            .Take(20)
            .ToListAsync(ct);

    // Moderators remove lists with offensive names.
    private static async Task<IResult> DeleteAsync(int id, KioskDbContext db, CancellationToken ct) =>
        await db.Wishlists.Where(w => w.Id == id).ExecuteDeleteAsync(ct) == 0 ? Results.NotFound() : Results.NoContent();
}
