using FluentAssertions.Common;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Shop.UnitTests;

[TestClass]
public static class AssemblyFixture
{
    // FluentAssertions 7 does not detect MSTest 4. This line reports its failures as MSTest assertion failures.
    [AssemblyInitialize]
    public static void RouteFluentAssertionsFailures(TestContext _)
        => Services.ThrowException = message => throw new AssertFailedException(message);
}
