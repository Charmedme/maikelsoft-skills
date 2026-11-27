namespace Kiosk.Api.Orders;

public sealed class Order
{
    public int Id { get; set; }

    public required string OrderNumber { get; set; }

    /// <summary>The customer account. Null for a guest checkout.</summary>
    public int? CustomerId { get; set; }

    public string? GuestEmail { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Placed;

    public string DeliveryAddress { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public int DiscountPercent { get; set; }

    public string? PartnerReference { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderLine> Lines { get; set; } = [];
}
