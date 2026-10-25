using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Documents;

public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    // Null: PuppeteerSharp reads PUPPETEER_EXECUTABLE_PATH (the chrome-headless-shell of the image).
    public string? ChromePath { get; set; }

    [Range(1, 32)]
    public int MaxConcurrentPages { get; set; } = 4;

    [Range(1_000, 120_000)]
    public int RenderTimeoutMs { get; set; } = 15_000;

    public string CompanyName { get; set; } = "Example Shop B.V.";

    public string CompanyAddress { get; set; } = "Havenstraat 12, 3011 AB Rotterdam";
}
