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
    public void Create_AddressesCustomerAndNotificationAddresses()
    {
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Business, "Example B.V.", s_now);
        customer.ReplaceNotificationAddresses(["goods-in@example.com", "ANN@example.com"]);

        var mail = ShippingNoticeMail.Create(customer, NewOrder("Back door"), DispatchedShipment(), Carrier("postnl"), s_now);

        mail.ToAddress.Should().Be("ann@example.com, goods-in@example.com");
        mail.Subject.Should().Be(ShippingNoticeMail.Subject);
    }

    [TestMethod]
    public void Create_ShowsTrackingLinkAndEncodesInstructions()
    {
        var customer = new Customer("Ann", "ann@example.com", CustomerKind.Consumer, null, s_now);

        var mail = ShippingNoticeMail.Create(customer, NewOrder("<b>Back door</b> & gate"), DispatchedShipment(), Carrier("postnl"), s_now);

        mail.HtmlBody.Should().Contain("3S123")
            .And.Contain("&lt;b&gt;Back door&lt;/b&gt;")
            .And.NotContain("<b>Back door</b>");
        mail.TextBody.Should().Contain("SKU-1").And.Contain("<b>Back door</b> & gate");
    }

    [TestMethod]
    [DataRow("ann@example.com", true)]
    [DataRow("\"x\r\nRCPT TO:<spy@evil.test>\"@example.com", false)]
    [DataRow("a@example.com, b@example.com", false)]
    [DataRow("Ann <ann@example.com>", false)]
    [DataRow("", false)]
    public void MailAddresses_IsValid(string value, bool expected)
    {
        MailAddresses.IsValid(value).Should().Be(expected);
    }

    private static Order NewOrder(string instructions)
    {
        var order = new Order(Guid.CreateVersion7(), s_now, null, instructions);
        order.AddLine("SKU-1", 1, 2m);
        return order;
    }

    private static Shipment DispatchedShipment()
    {
        var shipment = new Shipment(Guid.CreateVersion7(), s_now);
        shipment.Dispatch("postnl", "3S123", s_now);
        return shipment;
    }

    private static Carrier Carrier(string code)
    {
        CarrierCatalog.TryGet(code, out var carrier).Should().BeTrue();
        return carrier!;
    }
}
