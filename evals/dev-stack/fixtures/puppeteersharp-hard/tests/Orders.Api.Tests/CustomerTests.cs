using FluentAssertions;
using Orders.Api.Customers;

namespace Orders.Api.Tests;

[TestClass]
public sealed class CustomerTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Constructor_TrimsNameAndEmail()
    {
        var customer = new Customer("  Ann  ", " ann@example.com ", CustomerKind.Consumer, null, s_now);

        customer.Name.Should().Be("Ann");
        customer.Email.Should().Be("ann@example.com");
    }

    [TestMethod]
    public void Constructor_BusinessWithoutCompany_Throws()
    {
        var act = () => new Customer("Ann", "ann@example.com", CustomerKind.Business, " ", s_now);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void DisplayName_Business_ShowsCompanyAndContact()
    {
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Business, "Example B.V.", s_now);

        customer.DisplayName.Should().Be("Example B.V. (Ann)");
    }
}
