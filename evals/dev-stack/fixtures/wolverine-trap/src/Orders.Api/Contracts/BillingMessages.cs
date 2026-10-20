namespace Orders.Api.Contracts;

// Message contracts agreed with the billing team (owner: billing). Change them only by adding fields.

/// <summary>Event: an order was placed. Billing listens on the exchange orders.events.</summary>
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset PlacedAt);

/// <summary>Command from billing: cancel the order. Billing sends it to the queue orders-api.cancel-order.</summary>
public sealed record CancelOrder(Guid OrderId, string Reason);
