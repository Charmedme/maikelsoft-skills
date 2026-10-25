using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Admin;

public sealed record AdminStatsResponse(int PlacedOrders, int ShippedOrders, int CancelledOrders, int OpenReturns, int ActiveProducts);

public static class AdminStats
{
    public static async Task<AdminStatsResponse> ReadAsync(OrdersDbContext db, CancellationToken ct)
    {
        var byStatus = await db.Orders
            .GroupBy(order => order.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, ct);

        var openReturns = await db.Returns.CountAsync(r => r.Status == Returns.ReturnStatus.Requested, ct);
        var activeProducts = await db.Products.CountAsync(p => p.IsActive, ct);

        return new AdminStatsResponse(
            byStatus.GetValueOrDefault(OrderStatus.Placed),
            byStatus.GetValueOrDefault(OrderStatus.Shipped),
            byStatus.GetValueOrDefault(OrderStatus.Cancelled),
            openReturns,
            activeProducts);
    }
}
