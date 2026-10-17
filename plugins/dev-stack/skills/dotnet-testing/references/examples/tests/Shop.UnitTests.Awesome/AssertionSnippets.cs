using System.Net;
using AwesomeAssertions;

namespace Shop.UnitTests.Awesome;

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
    [TestMethod]
    public void Throw_ClosedOrder_MessageHasClosed()
    {
        Action act = () => throw new InvalidOperationException("Order 7 is closed");

        act.Should().Throw<InvalidOperationException>().WithMessage("*closed*").And.Message.Should().NotContain("open");
    }

    [TestMethod]
    public void BeEquivalentTo_ExcludeByName_IgnoresId()
    {
        var subject = new Order(1, 10m, null, ["a", "b"]);

        subject.Should().BeEquivalentTo(new Order(2, 10m, null, ["a", "b"]), options => options.ExcludingMembersNamed("Id").WithStrictOrdering());
    }

    [TestMethod]
    public void BeEquivalentTo_RuntimeTypes_ComparesDogMembers()
    {
        Animal subject = new Dog { Name = "Rex", Legs = 4 };

        subject.Should().BeEquivalentTo(new Dog { Name = "Rex", Legs = 4 }, options => options.PreferringRuntimeMemberTypes());
    }

    [TestMethod]
    public void Satisfy_Dog_ChecksMembers()
    {
        var dog = new Dog { Name = "Rex", Legs = 4 };

        dog.Should().Satisfy<Dog>(d =>
        {
            d.Name.Should().Be("Rex");
            d.Legs.Should().Be(4);
        });
    }

    [TestMethod]
    public void Response_Ok_StatusCodeIsOk()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
