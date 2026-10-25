using FluentAssertions;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ReturnPolicyTests
{
    private static readonly DateTimeOffset s_dispatched = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Check_NotShipped_ReturnsNotShipped()
    {
        ReturnPolicy.Check(null, s_dispatched, ReturnReason.Damaged).Should().Be(ReturnDecision.NotShipped);
    }

    [TestMethod]
    public void Check_AfterWindow_NoLongerNeeded_ReturnsWindowClosed()
    {
        ReturnPolicy.Check(s_dispatched, s_dispatched.AddDays(31), ReturnReason.NoLongerNeeded).Should().Be(ReturnDecision.WindowClosed);
    }

    [TestMethod]
    public void Check_AfterWindow_Damaged_ReturnsAllowed()
    {
        ReturnPolicy.Check(s_dispatched, s_dispatched.AddDays(90), ReturnReason.Damaged).Should().Be(ReturnDecision.Allowed);
    }

    [TestMethod]
    public void Decide_Twice_Throws()
    {
        var request = new ReturnRequest(Guid.CreateVersion7(), ReturnReason.Damaged, null, s_dispatched);
        request.Approve(s_dispatched);

        var act = () => request.Reject(s_dispatched);

        act.Should().Throw<InvalidOperationException>();
    }
}
