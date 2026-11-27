using System.Collections.Concurrent;

namespace Kiosk.Api.Admin;

public sealed class ExportScheduler
{
    private readonly ConcurrentDictionary<string, ExportSchedule> _schedules = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<ExportSchedule> All => _schedules.Values.ToList();

    public void Save(ExportSchedule schedule) => _schedules[schedule.Name] = schedule;
}
