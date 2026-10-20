namespace Orders.Api.Orders;

// The rules of the create request are in CreateOrderRequestValidator (FluentValidation).
public sealed record CreateOrderRequest(Guid? CustomerId, IReadOnlyList<CreateOrderLineRequest> Lines);

public sealed record CreateOrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

public sealed record OrderResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTimeOffset PlacedAt,
    DateTimeOffset? CancelledAt,
    decimal Total,
    IReadOnlyList<OrderLineResponse> Lines);

public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.PlacedAt,
            order.CancelledAt,
            order.Total,
            [.. order.Lines.Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice, line.LineTotal))]);
}
