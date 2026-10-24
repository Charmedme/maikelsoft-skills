namespace Orders.Api.Orders;

// Validated by CreateOrderRequestValidator. No DataAnnotations: with AddValidation they would answer first, without codes.
public sealed record CreateOrderRequest(Guid? CustomerId, IReadOnlyList<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTimeOffset PlacedAt,
    DateTimeOffset? CancelledAt,
    DateTimeOffset? ShippedAt,
    decimal Total,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
