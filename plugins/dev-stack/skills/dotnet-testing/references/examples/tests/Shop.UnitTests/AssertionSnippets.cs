using System.Net;
using FluentAssertions;
using FluentAssertions.Equivalency;

namespace Shop.UnitTests;

public class Animal
{
    public string Name { get; init; } = "";
}

public sealed class Dog : Animal
{
    public int Legs { get; init; }
}

[TestClass]
public sealed class AssertionSnippets
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Collections_ThreeLines_MatchExpected()
    {
        IReadOnlyList<string> lines = ["a", "b"];

        lines.Should().Equal("a", "b");
        lines.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public void BeEquivalentTo_AnonymousSubset_Passes()
    {
        var order = new Order(7, 10m, Now, ["a"]);

        order.Should().BeEquivalentTo(new { Id = 7, ClosedAt = Now }, options => options.ExcludingMissingMembers());
    }

    [TestMethod]
    public void Response_Ok_HasStatusCodeOk()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [TestMethod]
    public void BeEquivalentTo_RuntimeTypes_ComparesDogMembers()
    {
        Animal subject = new Dog { Name = "Rex", Legs = 4 };

        subject.Should().BeEquivalentTo(new Dog { Name = "Rex", Legs = 4 }, options => options.RespectingRuntimeTypes());
    }

    [TestMethod]
    public void BeEquivalentTo_ExcludeByName_IgnoresId()
    {
        var subject = new Order(1, 10m, null, ["a", "b"]);

        subject.Should().BeEquivalentTo(
            new Order(2, 10m, null, ["a", "b"]),
            options => options.Excluding((IMemberInfo m) => m.Name == "Id").WithStrictOrderingFor(o => o.Lines).AllowingInfiniteRecursion());
    }

    [TestMethod]
    public void Throw_WithoutMessage_UsesWhere()
    {
        Action act = () => throw new InvalidOperationException("Order 7 is closed");

        act.Should().Throw<Exception>().Where(e => !e.Message.Contains("open"));
    }
}
