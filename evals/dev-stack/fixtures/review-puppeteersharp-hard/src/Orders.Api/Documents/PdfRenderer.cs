using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Orders.Api.Documents;

// One headless Chrome for the whole process. A pick wave renders up to 40 packing slips at once, so the number
// of open pages is limited by Documents:MaxConcurrentPages.
public sealed class PdfRenderer : IAsyncDisposable
{
    private readonly DocumentOptions _options;
    private readonly SemaphoreSlim _pages;
    private readonly Lazy<Task<IBrowser>> _browser;

    public PdfRenderer(IOptions<DocumentOptions> options)
    {
        _options = options.Value;
        _pages = new SemaphoreSlim(_options.MaxConcurrentPages);
        _browser = new Lazy<Task<IBrowser>>(() => Puppeteer.LaunchAsync(new LaunchOptions
        {
            HeadlessMode = HeadlessMode.Shell,

            // Null: PuppeteerSharp reads PUPPETEER_EXECUTABLE_PATH.
            ExecutablePath = _options.ChromePath,
            Timeout = 30_000,
        }));
    }

    public async Task<byte[]> RenderAsync(string html, CancellationToken ct)
    {
        // The page methods take no CancellationToken. The token stops the wait for a free page.
        await _pages.WaitAsync(ct);
        try
        {
            var browser = await _browser.Value;
            await using var page = await browser.NewPageAsync();
            await page.SetJavaScriptEnabledAsync(false);
            await page.SetRequestInterceptionAsync(true);
            page.Request += async (_, e) =>
            {
                var url = e.Request.Url;
                if (url.StartsWith("data:", StringComparison.Ordinal) || url == "about:blank" || _options.AssetOrigins.Any(origin => url.StartsWith(origin, StringComparison.OrdinalIgnoreCase)))
                {
                    await e.Request.ContinueAsync();
                }
                else
                {
                    await e.Request.AbortAsync();
                }
            };

#pragma warning disable CS0618 // Networkidle0 waits until the logo and the web font are loaded.
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle0],
                Timeout = _options.RenderTimeoutMs,
            });
#pragma warning restore CS0618

            // DOC-01: A4 portrait, 15 mm margins.
            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                MarginOptions = new() { Top = "15mm", Bottom = "15mm", Left = "15mm", Right = "15mm" },
                Timeout = _options.RenderTimeoutMs,
            });
        }
        finally
        {
            _pages.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser.IsValueCreated)
        {
            var browser = await _browser.Value;
            await browser.DisposeAsync();
        }

        _pages.Dispose();
    }
}
