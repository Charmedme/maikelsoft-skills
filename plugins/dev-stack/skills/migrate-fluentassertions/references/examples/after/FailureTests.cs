using AwesomeAssertions;

namespace Shop.Tests;

[TestClass]
public sealed class FailureTests
{
    [TestMethod]
    public void Failure_IsReportedAsAssertFailedException()
    {
        Action act = () => 1.Should().Be(2);

        act.Should().Throw<AssertFailedException>();
    }
}
