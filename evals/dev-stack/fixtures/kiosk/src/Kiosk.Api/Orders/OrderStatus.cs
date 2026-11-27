namespace Kiosk.Api.Orders;

public enum OrderStatus
{
    Placed,
    AwaitingPayment,
    Paid,
    Shipped,
    Delivered,
    DamageReported,
    Cancelled,
}
