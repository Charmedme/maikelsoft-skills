---
name: puppeteersharp
description: HTML to PDF and page screenshots in .NET with PuppeteerSharp and headless Chrome. Use when code or a review renders an invoice, a report, or another HTML page to PDF, or touches PuppeteerSharp, Puppeteer.LaunchAsync, BrowserFetcher, chrome-headless-shell, PdfDataAsync, SetContentAsync, GoToAsync, --no-sandbox, Chrome in a Docker image, or user text in rendered HTML. Not for end-to-end UI tests with Playwright (dev-stack:dotnet-testing), PDF libraries without a browser (QuestPDF, PdfPig), or Node Puppeteer.
metadata:
  verified: "2026-10-25"
  versions: "PuppeteerSharp 25.15.0 (Chrome.DefaultBuildId 155.0.8059.84), chrome-headless-shell 155, .NET 10.0.13 (SDK 10.0.402), aspnet:10.0 (Ubuntu 24.04) images"
---

# PuppeteerSharp

HTML to PDF with PuppeteerSharp and headless Chrome: the browser lifetime, pages, concurrency, the PDF options, untrusted content, the container image, and the sandbox. The Dockerfile base rules are in the Docker skill. The secure coding rules (SSRF, output encoding) are in `dev-stack:secure-coding`. This skill adds what Chrome needs.

## First

1. Read `references/stack-rules.md`.
2. Find the `PuppeteerSharp` version in `*.csproj` or `Directory.Packages.props`. Each version has one Chrome build: `PuppeteerSharp.BrowserData.Chrome.DefaultBuildId` (155.0.8059.84 for 25.15.0, 155.0.8059.79 for 25.14.0). Upgrade the package and the Chrome build together.
3. Find where Chrome comes from: `ExecutablePath`, the environment variable `PUPPETEER_EXECUTABLE_PATH`, or `BrowserFetcher`. Without the first two, PuppeteerSharp looks in `<app folder>/ChromeHeadlessShell/Linux-<build>/`.
4. Find the content: own templates only, or text from users (a note, a name, an address). Text from users makes the HTML untrusted.
5. Find each `PdfOptions` in the existing code that the change touches. Find the page size of the team (for example a decision such as DOC-01). Check both against rules 15 and 16.

Done when you can name the PuppeteerSharp version, the Chrome build and its source, and whether the HTML holds user text. You can also name the page size and `PrintBackground` of each existing `PdfOptions`.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Renderer | PuppeteerSharp for HTML to PDF. Playwright when the team already runs it for end-to-end tests. | - |
| Browser | `chrome-headless-shell`, `HeadlessMode.Shell`, installed at image build time. | - |
| Content | HTML with user text is untrusted: encode it, inline all assets, block the network. | - |
| Sandbox | Non-root user, sandbox on. `--no-sandbox` only with an ADR and only for trusted templates. | SEC-24 |
| Image | The renderer gets its own image on full `aspnet:10.0`, not chiseled. | OPS-03 |
| Concurrency | 4 pages per process, from configuration. | - |

Existing code keeps its style (stack rule 3).

## Rules

### Browser and pages

1. Keep one browser per process in a singleton. It launches on first use, launches again when `IsConnected` is false, and closes at host shutdown (`IAsyncDisposable`). A launch costs 1 to 2 seconds and a process tree.
2. Use one `IBrowserContext` per job and one page per render, both with `await using`. The context isolates cookies and storage, and the dispose ends the renderer process.
3. Limit concurrent renders with a `SemaphoreSlim` (default 4) and let the rest wait. Each page is a renderer process, so unbounded renders end in an out-of-memory kill.
4. Set a timeout on each wait: `SetContentOptions.Timeout`, `PdfOptions.Timeout`, `WaitForSelectorOptions.Timeout`. The page methods take no `CancellationToken`: pass the token to the semaphore wait.
5. Return `PdfDataAsync` or `PdfStreamAsync`. Do not write a temp file with `PdfAsync(path)`.

```csharp
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
                    Allowlist = ["data:*", "about:blank"],     // rule 7: no network
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
```

Register it with `AddSingleton<InvoiceRenderer>()`. The host disposes it at shutdown.

### Untrusted content

6. Encode each user value with the template engine: `HtmlEncoder.Default.Encode(note)`, or a Razor component through `HtmlRenderer`. Concatenated user text is markup: it changes the layout and can load resources.
7. Block the network of the page when the HTML holds user text. Use `LaunchOptions.Allowlist = ["data:*", "about:blank"]` (Chrome 149 and later). Or use request interception that aborts each URL that is not `data:` or `about:blank`. Inline the CSS, fonts, and images (the logo too) as data URIs or embedded resources. Without the block, an `<img>`, `<link>`, `<script>`, `<iframe>`, or `<object>` makes the server request internal URLs (SSRF).
8. Turn JavaScript off (`SetJavaScriptEnabledAsync(false)`) when the document does not need it, and add a Content Security Policy `<meta>` tag (`default-src 'none'; img-src data:; style-src 'unsafe-inline'`).
9. Do not pass a URL from a request to `GoToAsync`. Render your own template. When a page must come from a URL, allow only a fixed list of `https` hosts. Never allow `file:`, `chrome:`, or an internal address.

Detail, the interception code, and the logo: `references/untrusted-content.md`.

### Container and sandbox

10. Run Chrome as a non-root user (`USER $APP_UID`) with the sandbox on (SEC-24). As root, Chrome refuses to start ("Running as root without --no-sandbox is not supported"). That is why generated code adds `--no-sandbox`. Without the sandbox, a renderer exploit runs code in your container.
11. Docker's default seccomp profile blocks the sandbox of a non-root Chrome ("No usable sandbox!"). Run the container with a seccomp profile that also allows `clone`, `clone3`, and `unshare`. Do not use `--no-sandbox`, `SYS_ADMIN`, or `seccomp=unconfined` instead.
12. Install `chrome-headless-shell` at image build time, pinned to `Chrome.DefaultBuildId`, and set `PUPPETEER_EXECUTABLE_PATH`. Do not call `BrowserFetcher.DownloadAsync()` at startup: each container then downloads Chrome over the network.
13. Build the renderer on full `aspnet:10.0` (OPS-03): a chiseled image has no package manager for the Chrome libraries. Install fonts (`fonts-liberation`, `fonts-noto-core`), set `LANG`, and start with an init process (`tini`), so that Chrome child processes are reaped.
14. Keep `HeadlessMode.Shell` (or `True`) outside a developer machine. No `Headless = false`, no `Devtools = true`.

The Dockerfile, the seccomp profile, and Kubernetes settings: `references/container.md`.

### PDF options

These defaults give a wrong PDF with no error. Existing code often has them.

15. Set `Format` (or `Width` and `Height`) in each `PdfOptions`. Without it, PuppeteerSharp prints Letter (612 x 792 points), not A4. A CSS `@page { size: A4 }` counts only with `PreferCSSPageSize = true`.
16. Set `PrintBackground = true` when the template has a background color or image. The default is `false`: header bars and row colors disappear, and white text on a dark bar becomes invisible.
17. Name each `PdfOptions` defect of existing code in the report (stack rule 3). Put a fixed defect under "Changes" and a defect that stays under "Open points".

The checked facts: `references/sources.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `Args = ["--no-sandbox", "--disable-setuid-sandbox"]` "for Docker" | `USER $APP_UID` and a seccomp profile that allows `clone`, `clone3`, `unshare` |
| `ENV PUPPETEER_DANGEROUS_NO_SANDBOX=true` | The same: non-root user, sandbox on |
| `await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);` | Build-time install and `PUPPETEER_EXECUTABLE_PATH` (the symbol does not exist: CS0117) |
| `await using var browser = await Puppeteer.LaunchAsync(...)` in each request | A singleton browser, a context per job |
| `await page.WaitForTimeoutAsync(500);` | A real wait condition (the method does not exist: CS1061) |
| `page.WaitForXPathAsync("//h1")` | `page.WaitForSelectorAsync("xpath///h1")` (CS0618) |
| `SetContentAsync(html, new NavigationOptions { WaitUntil = [Networkidle0] })` | `SetContentAsync(html, new SetContentOptions { Timeout = 15_000 })` (CS0618) |
| `await page.PdfAsync("invoice.pdf");` then read the file | `await page.PdfDataAsync(options)` |
| `await page.PdfDataAsync();` or `new PdfOptions { Timeout = 30_000 }` | `new PdfOptions { Format = PaperFormat.A4, PrintBackground = true, Timeout = 30_000 }` |
| `$"<p>{order.CustomerNote}</p>"` | `$"<p>{HtmlEncoder.Default.Encode(note)}</p>"` and a blocked network |
| `<img src="https://cdn.example.com/logo.png">` with a blocked network | The logo as a data URI or an embedded resource |
| `await page.GoToAsync(request.Url);` | Your own template, or an allow list of `https` hosts |
| `FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled` for the renderer | `FROM mcr.microsoft.com/dotnet/aspnet:10.0` plus libraries, fonts, `tini` |
| Node syntax: `headless: 'new'`, `page.pdf({ path })` | `HeadlessMode = HeadlessMode.Shell`, `PdfDataAsync` |

## Check

1. Build with the warnings as errors (CS0618 marks the obsolete APIs):

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Run the integration tests with `PUPPETEER_EXECUTABLE_PATH` set to the pinned `chrome-headless-shell`: `dotnet test`. Pass: the PDF starts with `%PDF-`, and its `/MediaBox` is A4 (about 596 x 842 points, not 612 x 792). A local HTTP listener gets no request from a note with `<img src="http://127.0.0.1:<port>/x">`. Content that never ends fails within the timeout. Test setup: `references/testing.md`.

3. Lint the Dockerfile: `hadolint Dockerfile`. Pass: no DL3002 (the last user is not root).

4. Smoke-test the image: run it with the production seccomp profile and render one page with the sandbox on. Pass: a PDF, and no "No usable sandbox" in the log.

5. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. No `--no-sandbox`, `--disable-setuid-sandbox`, or `PUPPETEER_DANGEROUS_NO_SANDBOX` without an ADR (SEC-24). tool (PPTR-NO-SANDBOX)
2. The image runs as a non-root user. tool (hadolint DL3002), judge
3. No `BrowserFetcher` download at runtime. Chrome is installed at build time and pinned. tool (PPTR-RUNTIME-DOWNLOAD), judge
4. One browser per process, a context and a page per render, disposed. tool (PPTR-BROWSER-PER-CALL), judge
5. Concurrent renders are limited. judge
6. User values are HTML-encoded. judge
7. The network of the page is blocked when the HTML holds user text. Assets are inlined. judge
8. No request URL reaches `GoToAsync` without a host allow list. judge
9. JavaScript is off when the document does not need it. judge
10. Timeouts on `SetContentAsync`, waits, and `PdfDataAsync`. tool (CS0618 for the old options), judge
11. `PdfDataAsync` or `PdfStreamAsync`, no temp file. tool (PPTR-PDF-FILE)
12. Headless in all non-local settings. tool (PPTR-HEADFUL)
13. The image has fonts, `LANG`, and an init process, on full `aspnet:10.0`. judge
14. An integration test proves the PDF, the blocked network, and the timeout. judge
15. Each `PdfOptions` sets the page size and `PrintBackground`, also in existing code that the change touches. tool (PPTR-PDF-FORMAT, PPTR-PDF-BACKGROUND), judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs the SEC-24 decision. It has the question, the options, and the recommendation.
- `references/untrusted-content.md`: read when the HTML holds user text, when the page loads a remote asset, or when code calls `GoToAsync`.
- `references/container.md`: read before you write or review a Dockerfile, a compose file, or a Kubernetes manifest for the renderer.
- `references/testing.md`: read before you write a test that renders a PDF, or set up CI for it.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language. Output follows `references/language.md`.
- `scripts/stackcheck.py`: the rule check of this skill.
