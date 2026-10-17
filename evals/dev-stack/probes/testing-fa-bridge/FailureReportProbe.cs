using FluentAssertions;

namespace Orders.Api.Tests.EvalProbes;

// Eval probe for dotnet-testing-trap. This test fails on purpose. The check reads how MSTest reports it.
[TestClass]
public sealed class FailureReportProbe
{
    [TestMethod]
    public void FluentAssertionFails() => 1.Should().Be(2);
}
