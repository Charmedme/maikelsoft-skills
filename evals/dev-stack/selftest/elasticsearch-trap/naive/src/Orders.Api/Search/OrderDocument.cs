using Orders.Api.Orders;

namespace Orders.Api.Search;

public sealed record OrderDocument(string Id, Guid CustomerId, string Status, DateTimeOffset PlacedAt, decimal Total, IReadOnlyList<string> Skus)
{
    public static OrderDocument From(Order order) =>
        new(order.Id.ToString(), order.CustomerId, order.Status.ToString(), order.PlacedAt, order.Total, [.. order.Lines.Select(line => line.Sku)]);
}
