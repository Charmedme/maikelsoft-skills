using Dapper;

namespace Orders.Api.Reports;

public sealed record ActiveStock(string Sku, int Available);

/// <summary>Stock of the SKUs that are still sold (not in discontinued_skus).</summary>
public sealed class DiscontinuedReport(ReportConnection connections)
{
    public async Task<IReadOnlyList<ActiveStock>> ActiveStockAsync(CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<ActiveStock>(new CommandDefinition(
            """
            SELECT s.sku AS Sku, s.available AS Available
            FROM stock_levels AS s
            WHERE s.sku NOT IN (SELECT d.sku FROM discontinued_skus AS d)
            ORDER BY s.sku
            """,
            cancellationToken: ct));
        return rows.AsList();
    }
}
