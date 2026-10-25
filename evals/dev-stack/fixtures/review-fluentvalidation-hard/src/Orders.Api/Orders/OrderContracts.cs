using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Orders;

public sealed record CreateOrderRequest(
    [property: Required] Guid? CustomerId,
    [property: Required, MinLength(1)] IReadOnlyList<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(
    [property: Required, StringLength(32, MinimumLength = 1)] string Sku,
    [property: Range(1, 1000)] int Quantity,
    [property: Range(typeof(decimal), "0", "100000")] decimal UnitPrice);

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
