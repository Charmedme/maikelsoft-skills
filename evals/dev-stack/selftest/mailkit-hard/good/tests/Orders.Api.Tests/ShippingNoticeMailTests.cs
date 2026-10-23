using FluentAssertions;
using Orders.Api.Customers;
using Orders.Api.Mail;
using Orders.Api.Orders;
using Orders.Api.Shipping;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ShippingNoticeMailTests
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 16, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Create_OneRowForEachRecipient_EncodesInstructions()
    {
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Business, "Example B.V.", s_now);
        customer.ReplaceNotificationAddresses(["goods-in@example.com"]);
        var order = new Order(Guid.CreateVersion7(), s_now, null, "<b>Back door</b> & gate");
        order.AddLine("SKU-1", 1, 2m);
        var shipment = new Shipment(Guid.CreateVersion7(), s_now);
        shipment.Dispatch("postnl", "3S123", s_now);
        CarrierCatalog.TryGet("postnl", out var carrier);

        var mails = ShippingNoticeMail.Create(customer, order, shipment, carrier!, s_now).ToList();

        mails.Select(m => m.ToAddress).Should().BeEquivalentTo(["ann@example.com", "goods-in@example.com"]);
        mails[0].HtmlBody.Should().Contain("3S123").And.Contain("&lt;b&gt;Back door&lt;/b&gt;").And.NotContain("<b>Back door</b>");
    }

    [TestMethod]
    [DataRow("ann@example.com", true)]
    [DataRow("\"x\r\nRCPT TO:<spy@evil.test>\"@example.com", false)]
    [DataRow("a@example.com, b@example.com", false)]
    [DataRow("Ann <ann@example.com>", false)]
    public void MailAddresses_IsValid(string value, bool expected)
    {
        MailAddresses.IsValid(value).Should().Be(expected);
    }
}
