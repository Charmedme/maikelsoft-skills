using AwesomeAssertions;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Shop.Tests;

[TestClass]
public static class AssemblyFixture
{
    [AssemblyInitialize]
    public static void Initialize(TestContext _)
        => AssertionConfiguration.Current.Equivalency.Modify(options => options.ComparingByMembers<Money>());
}
