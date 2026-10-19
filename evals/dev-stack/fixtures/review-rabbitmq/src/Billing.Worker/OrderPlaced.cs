namespace Billing.Worker;

public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset PlacedAt);
