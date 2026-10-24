using System.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Inventory;
using Orders.Api.Orders;

namespace Orders.Api.Picking;

public static class PickingEndpoints
{
    public static IEndpointRouteBuilder MapPicking(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/picking").WithTags("Picking");
        group.MapPost("/tasks", CreateTaskAsync);
        group.MapPost("/next", TakeNextAsync);
        group.MapPost("/{id:guid}/done", DoneAsync);
        group.MapGet("/", ListOpenAsync);
        group.MapPut("/zones/{sku}", PutZoneAsync);
        return app;
    }

    private static async Task<Results<Created<PickTaskResponse>, NotFound, Conflict<string>>> CreateTaskAsync(
        CreatePickTaskRequest request,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status != OrderStatus.Placed)
        {
            return TypedResults.Conflict("Only a placed order can be picked.");
        }

        if (await db.PickTasks.AnyAsync(t => t.OrderId == order.Id, ct))
        {
            return TypedResults.Conflict("The order already has a pick task.");
        }

        var firstSku = order.Lines[0].Sku;
        var zone = await db.PickZones.Where(z => z.Sku == firstSku).Select(z => z.Zone).FirstOrDefaultAsync(ct) ?? PickZone.Default;
        var task = new PickTask(order.Id, zone, time.GetUtcNow());
        db.PickTasks.Add(task);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/picking/{task.Id}", task.ToResponse());
    }

    private static async Task<Results<Ok<PickTaskResponse>, NoContent>> TakeNextAsync(
        TakeNextPickRequest request,
        PickTaskClaims claims,
        CancellationToken ct)
    {
        var task = await claims.TakeNextAsync(request.PickerId, request.Zone.Trim().ToUpperInvariant(), ct);
        return task is null ? TypedResults.NoContent() : TypedResults.Ok(task.ToResponse());
    }

    private static async Task<Results<Ok<PickTaskResponse>, NotFound, Conflict<string>>> DoneAsync(
        Guid id,
        OrdersDbContext db,
        TimeProvider time,
        CancellationToken ct)
    {
        // The task and the stock change in one transaction: the picked units leave the shelf.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var task = await db.PickTasks.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (task is null)
        {
            return TypedResults.NotFound();
        }

        if (task.Status != PickStatus.Picking)
        {
            return TypedResults.Conflict("The task is not being picked.");
        }

        task.MarkPicked(time.GetUtcNow());
        var lines = await db.Orders.Where(o => o.Id == task.OrderId).SelectMany(o => o.Lines).ToListAsync(ct);
        foreach (var line in lines)
        {
            var item = await db.StockItems.FirstAsync(s => s.Sku == line.Sku, ct);
            item.Ship(line.Quantity);
        }

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return TypedResults.Ok(task.ToResponse());
    }

    private static async Task<Ok<List<PickTaskResponse>>> ListOpenAsync(
        string zone,
        string? sort,
        OrdersDbContext db,
        CancellationToken ct)
    {
        var zoneParameter = new SqlParameter("@zone", SqlDbType.VarChar, 8) { Value = zone.Trim().ToUpperInvariant() };
        var orderBy = string.IsNullOrWhiteSpace(sort) ? "CreatedAt" : sort;
        var sql = $"SELECT TOP (100) * FROM dbo.PickTasks WHERE Status = 0 AND Zone = @zone ORDER BY {orderBy}";
        var tasks = await db.PickTasks
            .FromSqlRaw(sql, zoneParameter)
            .AsNoTracking()
            .ToListAsync(ct);

        return TypedResults.Ok(tasks.Select(t => t.ToResponse()).ToList());
    }

    private static async Task<Ok> PutZoneAsync(string sku, PutPickZoneRequest request, OrdersDbContext db, CancellationToken ct)
    {
        var skuParameter = new SqlParameter("@sku", SqlDbType.VarChar, 32) { Value = StockItem.Normalize(sku) };
        var zoneParameter = new SqlParameter("@zone", SqlDbType.VarChar, 8) { Value = request.Zone.Trim().ToUpperInvariant() };

        // One statement: the warehouse system sends the zone of a new SKU or a changed zone.
        await db.Database.ExecuteSqlAsync($"""
            MERGE dbo.PickZones AS t
            USING (VALUES ({skuParameter}, {zoneParameter})) AS s (Sku, Zone)
                ON t.Sku = s.Sku
            WHEN MATCHED THEN
                UPDATE SET Zone = s.Zone
            WHEN NOT MATCHED THEN
                INSERT (Sku, Zone) VALUES (s.Sku, s.Zone);
            """, ct);

        return TypedResults.Ok();
    }
}
