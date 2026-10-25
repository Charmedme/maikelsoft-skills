using FluentAssertions;
using Orders.Api.Common;

namespace Orders.Api.Tests;

[TestClass]
public sealed class DateRangeTests
{
    [TestMethod]
    [DataRow("2026-07-01", "2026-07-01", true)]
    [DataRow("2026-07-01", "2026-07-31", true)]
    [DataRow("2026-07-02", "2026-07-01", false)]
    [DataRow("2026-01-01", "2026-12-31", true)]
    [DataRow("2026-01-01", "2027-01-02", false)]
    public void IsValid(string from, string to, bool expected) =>
        DateRange.IsValid(DateOnly.Parse(from, System.Globalization.CultureInfo.InvariantCulture), DateOnly.Parse(to, System.Globalization.CultureInfo.InvariantCulture))
            .Should().Be(expected);
}
