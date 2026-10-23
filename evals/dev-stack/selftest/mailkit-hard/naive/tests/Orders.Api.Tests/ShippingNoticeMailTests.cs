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
        customer.ReplaceNotificationAddresses(["goods-in@example.com"]);
        var order = new Order(Guid.CreateVersion7(), s_now, null, "Back door");
        order.AddLine("SKU-1", 1, 2m);
        var shipment = new Shipment(Guid.CreateVersion7(), s_now);
        shipment.Dispatch("postnl", "3S123", s_now);
        CarrierCatalog.TryGet("postnl", out var carrier);

        using var message = ShippingNoticeMail.Create(customer, order, shipment, carrier!);

        message.To.Mailboxes.Select(m => m.Address).Should().BeEquivalentTo(["ann@example.com", "goods-in@example.com"]);
        message.HtmlBody.Should().Contain("3S123").And.Contain("Back door");
    }
}
