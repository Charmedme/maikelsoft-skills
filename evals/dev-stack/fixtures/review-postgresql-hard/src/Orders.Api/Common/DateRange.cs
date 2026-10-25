namespace Orders.Api.Common;

public static class DateRange
{
    public const int MaxDays = 366;

    /// <summary>True when 'to' is on or after 'from' and the range has at most <see cref="MaxDays"/> days.</summary>
    public static bool IsValid(DateOnly from, DateOnly to) =>
        to >= from && to.DayNumber - from.DayNumber < MaxDays;
}
