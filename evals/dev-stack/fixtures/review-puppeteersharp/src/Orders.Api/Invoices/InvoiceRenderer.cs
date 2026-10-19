using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Orders.Api.Invoices;

public sealed class InvoiceRenderer
{
    private readonly Task<string> _executablePath = DownloadChromeAsync();

    public async Task<byte[]> RenderHtmlAsync(string html, CancellationToken ct)
    {
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            HeadlessMode = HeadlessMode.Shell,
            ExecutablePath = await _executablePath,
        });
        await using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html, new NavigationOptions { WaitUntil = [WaitUntilNavigation.Networkidle0] });
        return await ToPdfAsync(page);
    }

    public async Task<byte[]> RenderUrlAsync(Uri url, CancellationToken ct)
    {
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            HeadlessMode = HeadlessMode.Shell,
            ExecutablePath = await _executablePath,
        });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync(url.ToString());
        return await ToPdfAsync(page);
    }

    private static async Task<byte[]> ToPdfAsync(IPage page)
    {
        var path = Path.GetTempFileName();
        try
        {
            await page.PdfAsync(path, new PdfOptions { Format = PaperFormat.A4, PrintBackground = true });
            return await File.ReadAllBytesAsync(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string> DownloadChromeAsync()
    {
        var installed = await new BrowserFetcher().DownloadAsync();
        return installed.GetExecutablePath();
    }
}
