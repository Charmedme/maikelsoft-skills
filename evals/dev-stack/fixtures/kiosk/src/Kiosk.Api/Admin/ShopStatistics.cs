using System.Globalization;
using System.Text;
using Kiosk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Kiosk.Api.Admin;

public sealed record ShopSummary(int Customers, int Orders, decimal Revenue, int OrdersToday, int OpenSupportConversations, IReadOnlyList<string> TopProducts);

public sealed class ShopStatistics(KioskDbContext db, TimeProvider time)
{
    public async Task<ShopSummary> SummaryAsync(CancellationToken ct)
    {
        var today = new DateTimeOffset(time.GetUtcNow().UtcDateTime.Date, TimeSpan.Zero);
        var top = await db.OrderLines.GroupBy(l => l.ProductName)
            .OrderByDescending(g => g.Sum(l => l.Quantity))
            .Take(3)
            .Select(g => g.Key)
            .ToListAsync(ct);
        return new ShopSummary(
            await db.Users.CountAsync(u => u.Role == Users.Roles.Customer, ct),
            await db.Orders.CountAsync(ct),
            await db.Orders.SumAsync(o => o.Total, ct),
            await db.Orders.CountAsync(o => o.CreatedAt >= today, ct),
            await db.SupportMessages.Select(m => m.ConversationId).Distinct().CountAsync(ct),
            top);
    }

    public async Task<string> PrometheusAsync(CancellationToken ct)
    {
        var summary = await SummaryAsync(ct);
        var text = new StringBuilder();
        text.AppendLine("# TYPE kiosk_customers gauge");
        text.AppendLine(CultureInfo.InvariantCulture, $"kiosk_customers {summary.Customers}");
        text.AppendLine("# TYPE kiosk_orders_total counter");
        text.AppendLine(CultureInfo.InvariantCulture, $"kiosk_orders_total {summary.Orders}");
        text.AppendLine("# TYPE kiosk_revenue_eur counter");
        text.AppendLine(CultureInfo.InvariantCulture, $"kiosk_revenue_eur {summary.Revenue}");
        return text.ToString();
    }
}
