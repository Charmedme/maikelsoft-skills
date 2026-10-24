using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Inventory;

// The nightly stock count of the warehouse: set OnHand of each counted SKU and add new SKUs.
public sealed class StockCountImport(OrdersDbContext db)
{
    public async Task<int> ApplyAsync(IReadOnlyList<StockCountLine> lines, CancellationToken ct)
    {
        // The last count of a SKU wins. MERGE fails when two source rows match one target row.
        var counts = lines
            .GroupBy(line => StockItem.Normalize(line.Sku))
            .Select(group => new { sku = group.Key, onHand = group.Last().OnHand });
        var json = new SqlParameter("@counts", SqlDbType.NVarChar, -1) { Value = JsonSerializer.Serialize(counts) };

        // One statement for the whole count. Receipts can arrive at the same time, so HOLDLOCK.
        return await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.StockItems WITH (HOLDLOCK) AS t
            USING OPENJSON({json}) WITH (Sku varchar(32) '$.sku', OnHand int '$.onHand') AS s
                ON t.Sku = s.Sku
            WHEN MATCHED THEN
                UPDATE SET OnHand = s.OnHand
            WHEN NOT MATCHED THEN
                INSERT (Sku, OnHand, Reserved) VALUES (s.Sku, s.OnHand, 0);
            """, ct);
    }
}
