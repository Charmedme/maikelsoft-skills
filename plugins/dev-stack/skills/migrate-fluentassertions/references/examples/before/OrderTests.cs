using System.Net;
using FluentAssertions;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace Shop.Tests;

[TestClass]
public sealed class OrderTests
{
    [TestMethod]
    public void BeEquivalentTo_RuntimeTypes_ComparesDogMembers()
    {
        Animal subject = new Dog { Name = "Rex", Legs = 4 };

        subject.Should().BeEquivalentTo(new Dog { Name = "Rex", Legs = 4 }, options => options.RespectingRuntimeTypes());
    }

    [TestMethod]
    public void BeEquivalentTo_TopLevelOnly_IgnoresAddress()
    {
        var subject = new Customer(1, "Ann", new Address("A", "B"));

        subject.Should().BeEquivalentTo(new Customer(1, "Ann", new Address("X", "Y")), options => options.ExcludingNestedObjects().Excluding(c => c.Address));
    }

    [TestMethod]
    public void BeEquivalentTo_ExcludeByName_IgnoresId()
    {
        var subject = new Customer(1, "Ann", new Address("A", "B"));

        subject.Should().BeEquivalentTo(new Customer(2, "Ann", new Address("A", "B")), options => options.Excluding((IMemberInfo m) => m.Name == "Id"));
    }

    [TestMethod]
    public void BeEquivalentTo_SharedOptions_IgnoresId()
    {
        var subject = new Customer(1, "Ann", new Address("A", "B"));

        subject.Should().BeEquivalentTo(new Customer(2, "Ann", new Address("A", "B")), IgnoreId);
    }

    [TestMethod]
    public void BeEquivalentTo_MoneyGlobalByMembers_ExcludesNote()
    {
        var subject = new Money(10m, "EUR", "a");

        subject.Should().BeEquivalentTo(new Money(10m, "EUR", "b"), options => options.Excluding(m => m.Note));
    }

    [TestMethod]
    public void Lines_ThreeItems_HasAtLeastTwo()
    {
        var order = new Order(1, 10m, ["a", "b", "c"]);

        order.Lines.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [TestMethod]
    public void Throw_AnyException_IsCaught()
    {
        Action act = () => throw new InvalidOperationException("Order 7 is closed");

        act.Should().Throw<Exception>().WithMessage("*closed*");
    }

    [TestMethod]
    public async Task ThrowAsync_ClosedOrder_ThrowsInvalidOperation()
    {
        Func<Task> act = () => Task.FromException(new InvalidOperationException("Order 7 is closed"));

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("*closed*");
    }

    [TestMethod]
    public void Response_Ok_HasStatusOk()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK);

        response.Should().HaveStatusCode(HttpStatusCode.OK);
    }

    [TestMethod]
    public void Scope_TwoFacts_BothPass()
    {
        var order = new Order(1, 10m, ["a"]);

        using (new AssertionScope())
        {
            order.Id.Should().Be(1);
            order.Total.Should().Be(10m);
        }
    }

    private static EquivalencyAssertionOptions<Customer> IgnoreId(EquivalencyAssertionOptions<Customer> options)
        => options.Excluding(c => c.Id);
}
