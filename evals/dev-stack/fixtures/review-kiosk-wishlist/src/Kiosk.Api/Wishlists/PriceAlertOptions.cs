namespace Kiosk.Api.Wishlists;

public sealed class PriceAlertOptions
{
    public const string Section = "PriceAlerts";

    /// <summary>Key of the text message provider. Without a key, alerts go out by mail only.</summary>
    public string SmsApiKey { get; set; } = string.Empty;

    public string TemplateRoot { get; set; } = "App_Data/wishlist-templates";
}
