using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Jobs;

public sealed class StaleOrderSweeper(IServiceProvider services, TimeProvider time, CancelPolicy policy) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var db = services.GetRequiredService<OrdersDbContext>();
            var cutoff = time.GetUtcNow() - (policy.Window * 7);
            var stale = await db.Orders
                .Where(order => order.Status == OrderStatus.Placed && order.PlacedAt < cutoff)
                .ToListAsync(stoppingToken);

            foreach (var order in stale)
            {
                order.Cancel(time.GetUtcNow(), TimeSpan.MaxValue);
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
