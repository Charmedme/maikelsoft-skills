using Microsoft.AspNetCore.Http.HttpResults;
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

    private static async Task<NoContent> PutAsync(
        string sku,
        PutStockRequest request,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var stock = await db.StockLevels.FindAsync([sku], ct);
        if (stock is null)
        {
            db.StockLevels.Add(new StockLevel(sku, request.Available));
        }
        else
        {
            stock.Replace(request.Available);
        }

        await db.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}
