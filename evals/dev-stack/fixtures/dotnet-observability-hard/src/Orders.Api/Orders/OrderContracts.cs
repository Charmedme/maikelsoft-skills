using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Orders;

public sealed record CreateOrderRequest(
    [property: Required] Guid? CustomerId,
    [property: Required, MinLength(1)] IReadOnlyList<CreateOrderLineRequest> Lines,
    [property: StringLength(64)] string? CustomerReference = null,
    [property: StringLength(500)] string? DeliveryInstructions = null,
    [property: Required] DeliveryAddressRequest? DeliveryAddress = null);

public sealed record DeliveryAddressRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string RecipientName,
    [property: Required, StringLength(200, MinimumLength = 1)] string Street,
    [property: Required, StringLength(16, MinimumLength = 2)] string PostalCode,
    [property: Required, StringLength(100, MinimumLength = 1)] string City,
    [property: Required, StringLength(2, MinimumLength = 2)] string CountryCode,
    [property: Phone, StringLength(32)] string? Phone = null)
{
    public DeliveryAddress ToAddress() => new(RecipientName.Trim(), Street.Trim(), PostalCode.Trim(), City.Trim(), CountryCode.ToUpperInvariant(), Phone?.Trim());
}

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
    string? CustomerReference,
    string? DeliveryInstructions,
    DeliveryAddress? DeliveryAddress,
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
            order.CustomerReference,
            order.DeliveryInstructions,
            order.DeliveryAddress,
            [.. order.Lines.Select(line => new OrderLineResponse(line.Sku, line.Quantity, line.UnitPrice, line.LineTotal))]);
}
