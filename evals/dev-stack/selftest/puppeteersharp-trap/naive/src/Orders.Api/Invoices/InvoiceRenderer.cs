using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Orders.Api.Invoices;

public sealed class InvoiceRenderer(IOptions<InvoiceOptions> options)
{
    public async Task<byte[]> RenderAsync(string html, CancellationToken ct)
    {
        var path = options.Value.ChromePath ?? Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (path is null)
        {
            var fetcher = new BrowserFetcher();
            var installed = await fetcher.DownloadAsync();
            path = installed.GetExecutablePath();
        }

        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = path,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"],
        });
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        return await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4 });
    }
}
