using FluentAssertions;
using Orders.Api.Customers;
using Orders.Api.Mail;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ReturnReceivedMailTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Create_AddressesTheCustomerAndEncodesTheComment()
    {
        var customer = new Customer("<b>Ann</b>", "ann@example.com", CustomerKind.Consumer, null, s_now);
        var request = new ReturnRequest(Guid.CreateVersion7(), ReturnReason.Damaged, "<i>Broken</i> lid", s_now);

        var mail = ReturnReceivedMail.Create(customer, request, s_now);

        mail.ToAddress.Should().Be("ann@example.com");
        mail.Subject.Should().Be(ReturnReceivedMail.Subject);
        mail.TextBody.Should().Contain("<i>Broken</i> lid");
        mail.HtmlBody.Should().Contain("&lt;i&gt;Broken&lt;/i&gt; lid").And.NotContain("<b>Ann</b>");
    }
}
