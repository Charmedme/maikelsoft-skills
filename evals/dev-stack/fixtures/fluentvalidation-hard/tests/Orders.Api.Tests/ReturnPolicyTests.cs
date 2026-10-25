using FluentAssertions;
using Orders.Api.Orders;
using Orders.Api.Returns;

namespace Orders.Api.Tests;

[TestClass]
public sealed class ReturnPolicyTests
{
    private static readonly DateTimeOffset s_placedAt = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan s_window = TimeSpan.FromDays(30);

    [TestMethod]
    public void Evaluate_NotShipped_IsRefused()
    {
        var order = OrderWith("SKU-1", 2);

        ReturnPolicy.Evaluate(order, "SKU-1", 1, 0, s_placedAt.AddDays(1), s_window).Should().Be(ReturnDecision.OrderNotShipped);
    }

    [TestMethod]
    public void Evaluate_InsideWindow_IsAllowed()
    {
        var order = ShippedOrderWith("SKU-1", 2);

        ReturnPolicy.Evaluate(order, "SKU-1", 2, 0, s_placedAt.AddDays(10), s_window).Should().Be(ReturnDecision.Allowed);
    }

    [TestMethod]
    public void Evaluate_AfterWindow_IsRefused()
    {
        var order = ShippedOrderWith("SKU-1", 2);

        ReturnPolicy.Evaluate(order, "SKU-1", 1, 0, s_placedAt.AddDays(40), s_window).Should().Be(ReturnDecision.WindowClosed);
    }

    [TestMethod]
    public void Evaluate_SkuNotOnOrder_IsRefused()
    {
        var order = ShippedOrderWith("SKU-1", 2);

        ReturnPolicy.Evaluate(order, "SKU-9", 1, 0, s_placedAt.AddDays(2), s_window).Should().Be(ReturnDecision.LineNotOnOrder);
    }

    [TestMethod]
    public void Evaluate_AlreadyReturnedCounts()
    {
        var order = ShippedOrderWith("SKU-1", 2);

        ReturnPolicy.Evaluate(order, "SKU-1", 1, 2, s_placedAt.AddDays(2), s_window).Should().Be(ReturnDecision.QuantityTooHigh);
    }

    private static Order OrderWith(string sku, int quantity)
    {
        var order = new Order(Guid.CreateVersion7(), s_placedAt);
        order.AddLine(sku, quantity, 1m);
        return order;
    }

    private static Order ShippedOrderWith(string sku, int quantity)
    {
        var order = OrderWith(sku, quantity);
        order.MarkShipped(s_placedAt.AddDays(1));
        return order;
    }
}
