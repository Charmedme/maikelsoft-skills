using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Documents;

public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    // Null: PuppeteerSharp reads PUPPETEER_EXECUTABLE_PATH, or the browser that ChromeInstaller downloaded.
    public string? ChromePath { get; set; }

    [Range(1, 32)]
    public int MaxConcurrentPages { get; set; } = 4;

    [Range(1_000, 120_000)]
    public int RenderTimeoutMs { get; set; } = 15_000;

    // Origins that a document page may load assets from (our logo and the web font). Every other request is aborted.
    public IReadOnlyList<string> AssetOrigins { get; set; } = ["https://cdn.example.com", "https://fonts.googleapis.com", "https://fonts.gstatic.com"];

    public string CompanyName { get; set; } = "Example Shop B.V.";

    public string CompanyAddress { get; set; } = "Havenstraat 12, 3011 AB Rotterdam";

    public Uri LogoUrl { get; set; } = new("https://cdn.example.com/logo.png");
}
