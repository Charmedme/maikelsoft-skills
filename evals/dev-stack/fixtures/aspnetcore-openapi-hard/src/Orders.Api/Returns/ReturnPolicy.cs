using Orders.Api.Orders;

namespace Orders.Api.Returns;

public enum ReturnDecision
{
    Allowed,
    OrderNotShipped,
    WindowClosed,
    LineNotOnOrder,
    QuantityTooHigh,
}

// A customer can return shipped goods inside the return window, at most the ordered quantity per SKU.
public static class ReturnPolicy
{
    public static ReturnDecision Evaluate(Order order, string sku, int quantity, int alreadyReturned, DateTimeOffset now, TimeSpan window)
    {
        if (order.Status != OrderStatus.Shipped || order.ShippedAt is not { } shippedAt)
        {
            return ReturnDecision.OrderNotShipped;
        }

        if (now - shippedAt > window)
        {
            return ReturnDecision.WindowClosed;
        }

        var ordered = order.QuantityOf(sku);
        if (ordered == 0)
        {
            return ReturnDecision.LineNotOnOrder;
        }

        return alreadyReturned + quantity > ordered ? ReturnDecision.QuantityTooHigh : ReturnDecision.Allowed;
    }

    public static string Describe(ReturnDecision decision) => decision switch
    {
        ReturnDecision.OrderNotShipped => "The order is not shipped yet.",
        ReturnDecision.WindowClosed => "The return window of the order is closed.",
        ReturnDecision.LineNotOnOrder => "The SKU is not on the order.",
        ReturnDecision.QuantityTooHigh => "The quantity is more than the customer can return.",
        _ => "The return is allowed.",
    };
}
