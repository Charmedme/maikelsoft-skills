using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Catalog;

/// <summary>Full-text search over name and description, ported from the old shop.</summary>
public sealed class ProductSearch(KioskDbContext db)
{
    private const int MaxTermLength = 200;

    public async Task<List<ProductSearchRow>> SearchAsync(string? term, CancellationToken ct)
    {
        var criteria = term ?? string.Empty;
        if (criteria.Length > MaxTermLength)
        {
            criteria = criteria[..MaxTermLength];
        }

        var pattern = "%" + EscapeLike(criteria) + "%";
        return await db.Database.SqlQuery<ProductSearchRow>(
            $"SELECT \"Id\", \"Name\", \"Description\", \"Price\", \"ImageUrl\" FROM \"Products\" WHERE ((\"Name\" ILIKE {pattern} OR \"Description\" ILIKE {pattern}) AND \"DeletedAt\" IS NULL) ORDER BY \"Name\"")
            .ToListAsync(ct);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);
}
