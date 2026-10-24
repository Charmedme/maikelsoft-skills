using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Orders.Api.Documents;

// One headless Chrome for the process (a singleton). Each render gets its own browser context and page.
// The pages hold customer text, so the page has no network: only data: URIs and about:blank load.
public sealed class PdfRenderer(IOptions<DocumentOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim _launch = new(1, 1);
    private readonly SemaphoreSlim _pages = new(options.Value.MaxConcurrentPages);
    private IBrowser? _browser;

    public async Task<byte[]> RenderAsync(string html, CancellationToken ct)
    {
        await _pages.WaitAsync(ct);
        try
        {
            var browser = await GetBrowserAsync(ct);
            await using var context = await browser.CreateBrowserContextAsync();
            await using var page = await context.NewPageAsync();
            await page.SetJavaScriptEnabledAsync(false);
            await page.SetContentAsync(html, new SetContentOptions { Timeout = options.Value.RenderTimeoutMs });

            // DOC-01: A4 portrait with 15 mm margins. PrintBackground keeps the header bar and the row colors.
            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new() { Top = "15mm", Bottom = "15mm", Left = "15mm", Right = "15mm" },
                Timeout = options.Value.RenderTimeoutMs,
            });
        }
        finally
        {
            _pages.Release();
        }
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _launch.WaitAsync(ct);
        try
        {
            if (_browser is not { IsConnected: true })
            {
                if (_browser is not null)
                {
                    await _browser.DisposeAsync();
                }

                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    HeadlessMode = HeadlessMode.Shell,
                    ExecutablePath = options.Value.ChromePath, // null: PUPPETEER_EXECUTABLE_PATH
                    Allowlist = ["data:*", "about:blank"],
                    Timeout = 30_000,
                });
            }

            return _browser;
        }
        finally
        {
            _launch.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _launch.Dispose();
        _pages.Dispose();
    }
}
