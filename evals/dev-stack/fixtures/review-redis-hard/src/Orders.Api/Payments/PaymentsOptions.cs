using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    [Required]
    public Uri BaseUrl { get; set; } = new("https://psp.example.com/");

    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "EUR";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;
}
