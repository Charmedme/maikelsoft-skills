namespace Kiosk.Api.Orders;

public sealed class PaymentOptions
{
    public const string Section = "Payments";

    public string Provider { get; set; } = "card-gateway";

    public string ProviderApiKey { get; set; } = string.Empty;
}
