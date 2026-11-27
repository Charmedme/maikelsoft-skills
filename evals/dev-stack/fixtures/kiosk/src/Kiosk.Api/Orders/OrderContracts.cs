namespace Kiosk.Api.Orders;

public sealed record OrderLineRequest(int ProductId, int Quantity);

public sealed record CreateOrderRequest(IReadOnlyList<OrderLineRequest> Lines, string DeliveryAddress, string? CouponCode);

public sealed record GuestOrderRequest(string Email, IReadOnlyList<OrderLineRequest> Lines, string DeliveryAddress);

public sealed record OrderLineDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

public sealed record OrderDto(
    int Id,
    string OrderNumber,
    string Status,
    decimal Total,
    int DiscountPercent,
    string DeliveryAddress,
    string? Note,
    DateTimeOffset CreatedAt,
    IReadOnlyList<OrderLineDto> Lines)
{
    public static OrderDto From(Order order) => new(
        order.Id,
        order.OrderNumber,
        order.Status.ToString(),
        order.Total,
        order.DiscountPercent,
        order.DeliveryAddress,
        order.Note,
        order.CreatedAt,
        order.Lines.Select(l => new OrderLineDto(l.ProductId, l.ProductName, l.Quantity, l.UnitPrice)).ToList());
}

public sealed record PlaceOrder(
    int? CustomerId,
    string? GuestEmail,
    IReadOnlyList<OrderLineRequest> Lines,
    string DeliveryAddress,
    string? CouponCode,
    string? PartnerReference = null,
    string? Note = null);

public sealed record PlaceOrderResult(Order? Order, string? Error);
