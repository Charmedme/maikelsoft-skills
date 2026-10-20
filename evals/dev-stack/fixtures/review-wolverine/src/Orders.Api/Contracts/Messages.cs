namespace Orders.Api.Contracts;

public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, decimal Total, DateTimeOffset PlacedAt);

public sealed record ShipOrder(Guid OrderId);

public sealed record OrderShipped(Guid OrderId, DateTimeOffset ShippedAt);

public sealed record CheckStock(string Sku, int Quantity);
