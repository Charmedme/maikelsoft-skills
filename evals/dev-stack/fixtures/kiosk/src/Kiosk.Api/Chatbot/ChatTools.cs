namespace Kiosk.Api.Chatbot;

public static class ChatTools
{
    public const string SearchProducts = "search_products";

    public const string GenerateCoupon = "generate_coupon";

    public static readonly IReadOnlyList<ChatToolDefinition> All =
    [
        new(SearchProducts, "Search the product catalog by keyword."),
        new(GenerateCoupon, "Generate a discount coupon for the customer. Only use this when the coupon policy conditions are met. Arguments: discount (percent), orderNumber."),
    ];
}
