using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Wishlists;

public sealed class WishlistQueries(KioskDbContext db)
{
    public IQueryable<Wishlist> WithItems() =>
        db.Wishlists.Include(w => w.Items).ThenInclude(i => i.Product);

    public async Task<List<WishlistSummary>> SearchPublicAsync(string term, CancellationToken ct) =>
        await db.Wishlists
            .FromSqlRaw($"SELECT * FROM \"Wishlists\" WHERE \"IsPublic\" AND \"Name\" ILIKE '%{term}%'")
            .Select(w => new WishlistSummary(w.Id, w.Name, w.Items.Count))
            .ToListAsync(ct);

    public async Task<List<WishlistSummary>> OfOwnerAsync(string email, CancellationToken ct) =>
        await db.Wishlists
            .FromSql($"SELECT w.* FROM \"Wishlists\" w JOIN \"Users\" u ON u.\"Id\" = w.\"OwnerId\" WHERE u.\"Email\" = {email}")
            .Select(w => new WishlistSummary(w.Id, w.Name, w.Items.Count))
            .ToListAsync(ct);
}
