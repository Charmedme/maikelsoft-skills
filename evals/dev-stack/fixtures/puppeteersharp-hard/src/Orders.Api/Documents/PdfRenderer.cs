using Microsoft.Extensions.Options;
using PuppeteerSharp;

namespace Orders.Api.Documents;

public sealed class PdfRenderer(IOptions<DocumentOptions> options)
{
    public async Task<byte[]> RenderAsync(string html, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = options.Value.ChromePath,

            // Chrome does not start in our container without these two flags.
            Args = ["--no-sandbox", "--disable-dev-shm-usage"],
        });
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        return await page.PdfDataAsync(new PdfOptions
        {
            MarginOptions = new() { Top = "15mm", Bottom = "15mm", Left = "15mm", Right = "15mm" },
        });
    }
}
