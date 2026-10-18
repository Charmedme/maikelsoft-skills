using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;

namespace Shop.UnitTests.Awesome;

[TestClass]
public sealed class ReservationTests
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 16, 9, 0, 0, TimeSpan.Zero));

    [TestMethod]
    public void IsExpired_BeforeHoldTime_ReturnsFalse()
    {
        var reservation = new Reservation(_time.GetUtcNow(), _time);
        _time.Advance(Reservation.HoldTime - TimeSpan.FromSeconds(1));

        bool expired = reservation.IsExpired;

        expired.Should().BeFalse();
    }

    [TestMethod]
    public void IsExpired_AtHoldTime_ReturnsTrue()
    {
        var reservation = new Reservation(_time.GetUtcNow(), _time);
        _time.Advance(Reservation.HoldTime);

        bool expired = reservation.IsExpired;

        expired.Should().BeTrue();
    }
}
