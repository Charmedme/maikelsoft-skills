namespace Orders.Api.Orders;

public enum CancelOutcome
{
    Cancelled,
    AlreadyCancelled,
    AlreadyShipped,
    WindowClosed,
}
