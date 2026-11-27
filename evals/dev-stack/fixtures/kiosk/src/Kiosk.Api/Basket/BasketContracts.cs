namespace Kiosk.Api.Basket;

public sealed record BasketDto(int Id, string? CouponCode, IReadOnlyList<BasketItemDto> Items, decimal Total);

public sealed record BasketItemDto(int Id, int ProductId, string Name, int Quantity, decimal UnitPrice);

public sealed record AddItemRequest(int ProductId, int Quantity);

public sealed record CheckoutRequest(string DeliveryAddress, string? CouponCode);
