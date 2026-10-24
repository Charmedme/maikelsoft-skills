using FluentAssertions;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CustomerTests
{
    [TestMethod]
    public void Constructor_TrimsNameAndEmail()
    {
        var customer = new Customer("  Ann  ", " ann@example.com ", DateTimeOffset.UnixEpoch);

        customer.Name.Should().Be("Ann");
        customer.Email.Should().Be("ann@example.com");
    }

    [TestMethod]
    public void Rename_Empty_Throws()
    {
        var customer = new Customer("Ann", "ann@example.com", DateTimeOffset.UnixEpoch);

        var act = () => customer.Rename("");

        act.Should().Throw<ArgumentException>();
    }
}
