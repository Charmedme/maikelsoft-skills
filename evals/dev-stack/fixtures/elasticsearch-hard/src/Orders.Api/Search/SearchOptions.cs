using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Search;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    [Required]
    public Uri? Uri { get; set; }

    [Required]
    public string ApiKey { get; set; } = string.Empty; // user secrets or Search__ApiKey

    // SHA-256 fingerprint of the cluster CA. Empty for a cluster with a public certificate.
    public string? CertificateFingerprint { get; set; }

    [Range(1, 120)]
    public int RequestTimeoutSeconds { get; set; } = 10;
}
