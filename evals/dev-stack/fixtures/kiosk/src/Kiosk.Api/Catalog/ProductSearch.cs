using System.Text;
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

        var sql = new StringBuilder("SELECT \"Id\", \"Name\", \"Description\", \"Price\", \"ImageUrl\" FROM \"Products\" ");
        sql.Append("WHERE ((\"Name\" ILIKE '%").Append(criteria).Append("%' OR \"Description\" ILIKE '%").Append(criteria).Append("%') ");
        sql.Append("AND \"DeletedAt\" IS NULL) ORDER BY \"Name\"");
        return await db.Database.SqlQueryRaw<ProductSearchRow>(sql.ToString()).ToListAsync(ct);
    }
}
