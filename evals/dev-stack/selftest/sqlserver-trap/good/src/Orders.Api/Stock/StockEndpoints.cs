using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;

namespace Orders.Api.Stock;

public static class StockEndpoints
{
    public static IEndpointRouteBuilder MapStock(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/stock").WithTags("Stock");
        group.MapPut("/{sku}", PutAsync);
        return app;
    }

    private static async Task<Results<NoContent, ValidationProblem>> PutAsync(
        string sku,
        PutStockRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        if (sku.Length is 0 or > 32)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(sku)] = ["The SKU must have 1 to 32 characters."],
            });
        }

        // HOLDLOCK keeps the key range locked until the end of the statement, so two calls
        // for a new SKU cannot both insert. The parameter type matches the varchar(32) key.
        var skuParameter = new SqlParameter("@sku", SqlDbType.VarChar, 32) { Value = sku };
        await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.StockLevels WITH (HOLDLOCK) AS t
            USING (VALUES ({skuParameter}, {request.Available})) AS s (Sku, Available)
                ON t.Sku = s.Sku
            WHEN MATCHED THEN
                UPDATE SET Available = s.Available
            WHEN NOT MATCHED THEN
                INSERT (Sku, Available) VALUES (s.Sku, s.Available);
            """, ct);
        return TypedResults.NoContent();
    }
}
