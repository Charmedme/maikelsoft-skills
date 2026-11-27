namespace Kiosk.Api.Chatbot;

public sealed class Coupon
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public int DiscountPercent { get; set; }

    public int? UserId { get; set; }

    public string? OrderNumber { get; set; }

    public string Source { get; set; } = "chat";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RedeemedAt { get; set; }
}
