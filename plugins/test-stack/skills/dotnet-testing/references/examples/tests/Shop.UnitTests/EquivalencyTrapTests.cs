using FluentAssertions;

namespace Shop.UnitTests;

[TestClass]
public sealed class EquivalencyTrapTests
{
    [TestMethod]
    public void BeEquivalentTo_DefaultOptions_IgnoresCollectionOrder()
    {
        var actual = new Order(1, 1m, null, ["a", "b"]);

        actual.Should().BeEquivalentTo(new Order(1, 1m, null, ["b", "a"]));
    }

    [TestMethod]
    public void BeEquivalentTo_StrictOrdering_FailsOnOtherOrder()
    {
        var actual = new Order(1, 1m, null, ["a", "b"]);

        Action act = () => actual.Should().BeEquivalentTo(new Order(1, 1m, null, ["b", "a"]), options => options.WithStrictOrdering());

        act.Should().Throw<AssertFailedException>();
    }

    [TestMethod]
    public void BeEquivalentTo_TypeWithEqualsAndExcluding_StillComparesByValue()
    {
        var actual = new Money(10m, "EUR", "note-a");

        Action act = () => actual.Should().BeEquivalentTo(new Money(10m, "EUR", "note-b"), options => options.Excluding(m => m.Note));

        act.Should().Throw<AssertFailedException>("Money overrides Equals, so the Excluding call has no effect");
    }

    [TestMethod]
    public void BeEquivalentTo_ComparingByMembers_ExcludesNote()
    {
        var actual = new Money(10m, "EUR", "note-a");

        actual.Should().BeEquivalentTo(
            new Money(10m, "EUR", "note-b"),
            options => options.ComparingByMembers<Money>().Excluding(m => m.Note));
    }
}
