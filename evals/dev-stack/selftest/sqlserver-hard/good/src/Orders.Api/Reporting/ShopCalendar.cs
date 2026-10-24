namespace Orders.Api.Reporting;

// The shop is Dutch: a report day is a day in Amsterdam time.
public static class ShopCalendar
{
    private static readonly TimeZoneInfo s_zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

    // The start of an Amsterdam day as a UTC value, for a range on a datetime2 column that holds UTC.
    public static DateTime StartOfDayUtc(DateOnly day) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), s_zone);
}
