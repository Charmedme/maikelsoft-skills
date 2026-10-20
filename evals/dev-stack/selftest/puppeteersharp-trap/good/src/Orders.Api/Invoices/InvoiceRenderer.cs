using Microsoft.Extensions.Options;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Orders.Api.Invoices;

/// <summary>One browser per process, one context and one page per render, at most N pages at a time.</summary>
public sealed class InvoiceRenderer(IOptions<InvoiceOptions> options) : IAsyncDisposable
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
            await page.SetContentAsync(html, new SetContentOptions { Timeout = 15_000 });
            return await page.PdfDataAsync(new PdfOptions { Format = PaperFormat.A4, PrintBackground = true, Timeout = 30_000 });
        }
        finally
        {
            _pages.Release();
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

    private async Task<IBrowser> GetBrowserAsync(CancellationToken ct)
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await _launch.WaitAsync(ct);
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            if (_browser is not null)
            {
                await _browser.DisposeAsync();
            }

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                HeadlessMode = HeadlessMode.Shell,
                ExecutablePath = options.Value.ChromePath,
                Allowlist = ["data:*", "about:blank"],
                Timeout = 30_000,
            });
            return _browser;
        }
        finally
        {
            _launch.Release();
        }
    }
}
