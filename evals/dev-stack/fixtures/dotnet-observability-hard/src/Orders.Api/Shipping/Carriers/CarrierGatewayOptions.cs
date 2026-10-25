using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Shipping.Carriers;

public sealed class CarrierGatewayOptions
{
    public const string SectionName = "CarrierGateway";

    [Required]
    public Uri? BaseUrl { get; set; }

    // From user secrets or the environment (CarrierGateway__ApiKey).
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 15;
}
