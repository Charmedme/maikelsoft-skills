using Dapper;

namespace Orders.Api.Reports;

public sealed record SkuSales(string Sku, DateOnly Day, int Quantity, decimal Revenue);

/// <summary>Sales per SKU and per day (Amsterdam time) for a set of orders.</summary>
public sealed class SalesReport(ReportConnection connections)
{
    public async Task<IReadOnlyList<SkuSales>> ForOrdersAsync(IReadOnlyList<Guid> orderIds, CancellationToken ct)
    {
        await using var connection = await connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<SkuSales>(new CommandDefinition(
            """
            SELECT l."Sku" AS Sku,
                   o."PlacedAt"::date AS Day,
                   sum(l."Quantity")::int AS Quantity,
                   sum(l."Quantity" * l."UnitPrice") AS Revenue
            FROM orders AS o
            JOIN order_lines AS l ON l."OrderId" = o."Id"
            WHERE o."Id" IN @orderIds
            GROUP BY l."Sku", o."PlacedAt"::date
            ORDER BY Day, Sku
            """,
            new { orderIds },
            cancellationToken: ct));
        return rows.AsList();
    }
}
