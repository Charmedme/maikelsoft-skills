using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Shipping;

public sealed class CarrierOptions
{
    public const string SectionName = "Carrier";

    // The parcel platform that books with PostNL, DHL and DPD for us. See docs/carrier-api.md.
    [Required]
    public Uri BaseUrl { get; set; } = new("https://parcels.example.com/");

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;
}
