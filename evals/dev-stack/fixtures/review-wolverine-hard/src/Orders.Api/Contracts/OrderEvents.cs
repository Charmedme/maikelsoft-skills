using Wolverine.Attributes;

namespace Orders.Api.Contracts;

// Events that this service publishes on the exchange orders.events.

[MessageIdentity("orders.order-paid", Version = 1)]
public sealed record OrderPaid(Guid OrderId, Guid CustomerId, decimal Amount, string Currency, DateTimeOffset PaidAt);

[MessageIdentity("orders.order-cancelled", Version = 1)]
public sealed record OrderCancelled(Guid OrderId, Guid CustomerId, DateTimeOffset CancelledAt);

[MessageIdentity("orders.shipment-booked", Version = 1)]
public sealed record ShipmentBooked(Guid ShipmentId, Guid OrderId, string Carrier, string TrackingNumber);
