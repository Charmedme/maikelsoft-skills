namespace Orders.Api.Infrastructure;

// Stable codes in the "code" extension of each ProblemDetails response. Clients act on these codes.
public static class ProblemCodes
{
    public const string OrderNotFound = "order.not_found";
    public const string OrderAlreadyShipped = "order.already_shipped";
    public const string OrderCancelWindowClosed = "order.cancel_window_closed";
    public const string OrderNotShipped = "order.not_shipped";
    public const string ProductNotFound = "product.not_found";
    public const string CustomerNotFound = "customer.not_found";
    public const string CustomerEmailTaken = "customer.email_taken";
    public const string ReturnNotFound = "return.not_found";
    public const string ReturnNotAllowed = "return.not_allowed";
    public const string ShipmentNotFound = "shipment.not_found";
    public const string IdempotencyKeyMissing = "request.idempotency_key_missing";
    public const string IdempotencyKeyReused = "request.idempotency_key_reused";
}
