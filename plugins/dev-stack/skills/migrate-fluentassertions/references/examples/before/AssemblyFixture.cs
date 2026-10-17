using FluentAssertions;
using FluentAssertions.Common;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Shop.Tests;

[TestClass]
public static class AssemblyFixture
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
    {
        // FluentAssertions 7 does not detect MSTest 4. Route failures to MSTest.
        Services.ThrowException = message => throw new AssertFailedException(message);
        AssertionOptions.AssertEquivalencyUsing(options => options.ComparingByMembers<Money>());
    }
}
