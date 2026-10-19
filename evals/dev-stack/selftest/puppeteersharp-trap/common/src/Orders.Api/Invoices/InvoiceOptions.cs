namespace Orders.Api.Invoices;

public sealed class InvoiceOptions
{
    public const string SectionName = "Invoices";

    /// <summary>Path of chrome-headless-shell. Null: PuppeteerSharp reads PUPPETEER_EXECUTABLE_PATH.</summary>
    public string? ChromePath { get; set; }

    public int MaxConcurrentPages { get; set; } = 4;
}
