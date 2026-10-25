using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Orders;

namespace Orders.Api.Reporting;

public sealed class SalesReportService(OrdersDbContext db)
{
    public const int MaxDays = 92;

    public async Task<SalesReport> BuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        if (to < from)
        {
            throw new ArgumentException("The end date is before the start date.", nameof(to));
        }

        if (to.DayNumber - from.DayNumber + 1 > MaxDays)
        {
            throw new ArgumentException($"A report covers at most {MaxDays} days.", nameof(to));
        }

        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var orders = await db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .Where(o => o.PlacedAt >= start && o.PlacedAt < end)
            .ToListAsync(ct);

        var days = new List<SalesSummary>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var ofDay = orders.Where(o => DateOnly.FromDateTime(o.PlacedAt.UtcDateTime) == day).ToList();
            var kept = ofDay.Where(o => o.Status != OrderStatus.Cancelled).ToList();
            days.Add(new SalesSummary(day, ofDay.Count, ofDay.Count - kept.Count, kept.Sum(o => o.Total)));
        }

        return new SalesReport(from, to, days, days.Sum(d => d.NetRevenue));
    }
}
