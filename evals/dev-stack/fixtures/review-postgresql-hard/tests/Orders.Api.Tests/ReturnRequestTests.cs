using FluentAssertions;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ReturnRequestTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void MarkReceived_Requested_SetsReceived()
    {
        var request = new ReturnRequest(Guid.CreateVersion7(), "Broken", s_now);

        request.MarkReceived(s_now.AddDays(2)).Should().BeTrue();

        request.Status.Should().Be(ReturnStatus.Received);
        request.ReceivedAt.Should().Be(s_now.AddDays(2));
    }

    [TestMethod]
    public void MarkReceived_Twice_ReturnsFalse()
    {
        var request = new ReturnRequest(Guid.CreateVersion7(), "Broken", s_now);
        request.MarkReceived(s_now);

        request.MarkReceived(s_now.AddDays(1)).Should().BeFalse();
    }
}
