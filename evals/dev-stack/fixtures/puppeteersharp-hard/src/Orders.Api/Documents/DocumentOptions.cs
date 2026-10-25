namespace Orders.Api.Documents;

public sealed class DocumentOptions
{
    public const string SectionName = "Documents";

    // Null: PuppeteerSharp reads PUPPETEER_EXECUTABLE_PATH, or the browser that ChromeInstaller downloaded.
    public string? ChromePath { get; set; }

    public string CompanyName { get; set; } = "Example Shop B.V.";

    public string CompanyAddress { get; set; } = "Havenstraat 12, 3011 AB Rotterdam";

    public Uri LogoUrl { get; set; } = new("https://cdn.example.com/logo.png");
}
