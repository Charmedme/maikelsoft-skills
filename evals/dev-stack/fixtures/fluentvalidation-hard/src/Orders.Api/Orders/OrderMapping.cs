namespace Orders.Api.Orders;

public static class OrderMapping
{
    public static OrderResponse ToResponse(this Order order) =>
        new(
            order.Id,
            order.CustomerId,
            order.Status.ToString(),
            order.PlacedAt,
            order.CancelledAt,
            order.ShippedAt,
            order.Total,
            [.. order.Lines.Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice, line.LineTotal))]);
}
