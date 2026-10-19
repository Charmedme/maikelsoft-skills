using System.ComponentModel.DataAnnotations;
using Orders.Api.Mail;

namespace Orders.Api.Orders;

public sealed record CreateOrderRequest(
    [property: Required] Guid? CustomerId,
    [property: Required, MinLength(1)] IReadOnlyList<CreateOrderLineRequest> Lines,
    [property: Required, StringLength(200, MinimumLength = 1)] string CustomerName,
    [property: Required, StringLength(254), MailAddress] string Email);

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
