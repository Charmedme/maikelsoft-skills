# Sources

Checked on 2026-10-19, and again on 2026-10-25. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Checked on 2026-10-25
- Credits

## Versions

| Item | Version on 2026-10-27 | Source |
|------|-----------------------|--------|
| PuppeteerSharp, PuppeteerSharp.Cdp | 25.15.0 (MIT, net8.0, net10.0, netstandard2.0). 25.14.0 was the version of 2026-10-19 | [nuget.org PuppeteerSharp](https://www.nuget.org/packages/PuppeteerSharp) |
| Chrome build of PuppeteerSharp 25.15.0 | 155.0.8059.84 (`Chrome.DefaultBuildId`). 25.14.0 has 155.0.8059.79 | [BrowserData/Chrome.cs at v25.15.0](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/BrowserData/Chrome.cs) |
| chrome-headless-shell linux64 | 155.0.8059.84. Chrome for Testing Stable is 156.0.8078.47 on 2026-10-27 | [Chrome for Testing](https://googlechromelabs.github.io/chrome-for-testing/), [last-known-good-versions.json](https://googlechromelabs.github.io/chrome-for-testing/last-known-good-versions.json) |
| Microsoft.Playwright (alternative) | 1.64.1 (MIT) | [nuget.org](https://www.nuget.org/packages/Microsoft.Playwright) |
| .NET runtime images | `mcr.microsoft.com/dotnet/aspnet:10.0` (Ubuntu 24.04), user `app` 1654 | [.NET container images](https://learn.microsoft.com/dotnet/core/docker/container-images) |
| .NET 10 | Runtime 10.0.14 (a security release of 2026-10-25), SDK 10.0.403 | [.NET 10 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) |

## Documentation

- [PuppeteerSharp documentation](https://www.puppeteersharp.com/): launch options, `HeadlessMode`, PDF options.
- [Puppeteer troubleshooting](https://pptr.dev/troubleshooting): running in Docker, the sandbox, fonts, an init process.
- [Chromium Linux sandboxing](https://chromium.googlesource.com/chromium/src/+/main/docs/linux/sandboxing.md) and [AppArmor user namespace restrictions](https://chromium.googlesource.com/chromium/src/+/main/docs/security/apparmor-userns-restrictions.md).
- [Docker seccomp security profiles](https://docs.docker.com/engine/security/seccomp/): the default profile, `clone` and `unshare` with `CAP_SYS_ADMIN`.
- [Kubernetes seccomp](https://kubernetes.io/docs/tutorials/security/seccomp/): `Localhost` profiles.
- [Playwright Docker](https://playwright.dev/docs/docker): `--init`, not for untrusted sites without a sandbox.
- [OWASP SSRF Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html).
- [URLPattern](https://urlpattern.spec.whatwg.org/): the pattern syntax of `Allowlist` and `BlockList`.

## Checked by compile and run

With SDK 10.0.402, PuppeteerSharp 25.14.0, and chrome-headless-shell 155.0.8059.79 on 2026-10-19:

- The renderer in `SKILL.md` and the code in `untrusted-content.md` and `testing.md` compile with `TreatWarningsAsErrors`.
- PuppeteerSharp reads `PUPPETEER_EXECUTABLE_PATH` when `ExecutablePath` is null. Without both, it looks in `<app folder>/ChromeHeadlessShell/Linux-155.0.8059.79/` (ran).
- As root without `--no-sandbox`: "Running as root without --no-sandbox is not supported" (ran).
- `Allowlist = ["data:*", "about:blank"]` blocked all five tags (`img`, `link`, `script`, `iframe`, `object`) of an unencoded note. Without the block and the encoding, each tag made a request (ran, with a local listener).
- An encoded note of ten page-break elements stays on one page. Unencoded, the PDF has more pages (ran).
- `SetContentAsync(string, NavigationOptions)` is obsolete: CS0618, "The networkidle0 and networkidle2 wait conditions never worked reliably with SetContent" (compiled).
- `WaitForTimeoutAsync` does not exist (CS1061). `BrowserFetcher.DefaultChromiumRevision` does not exist (CS0117). `WaitForXPathAsync` is obsolete (CS0618). From the concept lab of 2026-10-19.
- The page methods (`SetContentAsync`, `PdfDataAsync`, `GoToAsync`) take no `CancellationToken` (XML documentation of 25.14.0).
- A `WaitForSelectorAsync` timeout throws `WaitTaskTimeoutException`, a `PuppeteerException` and not a `TimeoutException` (ran).
- `HtmlEncoder.Default.Encode("<b>x</b>")` gives `&lt;b&gt;x&lt;/b&gt;` (ran).
- The PuppeteerSharp assembly reads `PUPPETEER_DANGEROUS_NO_SANDBOX`.
- Docker image: the runtime stage of `container.md`, with Chrome copied from a local zip. The build container had no access to the download host. As user `app` with the default seccomp profile, "No usable sandbox!". With the default profile plus `clone`, `clone3`, `unshare`, a PDF. With `clone`, `clone3` only, "No usable sandbox!" (ran).

Not checked: the full `docker build` with the `curl` download stage, because the build container could not verify TLS through the proxy of the lab. Also not checked: the Kubernetes `RuntimeDefault` profile, and AppArmor hosts.

## Checked on 2026-10-25

With SDK 10.0.402, PuppeteerSharp 25.15.0, and chrome-headless-shell 155.0.8059.84:

| Fact | Source |
|------|--------|
| 25.15.0 has no breaking change against 25.14.0. It adds `Browser.RestrictNetworkAsync`, `Page.TabInfoAsync`, and `WsOptions.HandshakeTimeout`, and rolls Chrome to 155.0.8059.84. | [git log v25.14.0..v25.15.0](https://github.com/hardkoded/puppeteer-sharp/compare/v25.14.0...v25.15.0) |
| `PdfOptions.Format` has no default value. Without `Format`, `Width`, and `Height`, `CdpPage` uses `PaperFormat.Letter`. A PDF from `new PdfOptions()` has `/MediaBox [0 0 612 792]` (ran). | [CdpPage.cs at v25.15.0](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/Cdp/CdpPage.cs), [PdfOptions.cs](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/PdfOptions.cs) |
| `Format = PaperFormat.A4` gives `/MediaBox [0 0 595.91998 841.91998]` (ran). | Ran |
| A CSS `@page { size: A4 }` without `PreferCSSPageSize = true` gives Letter. With `PreferCSSPageSize = true`, it gives `[0 0 594.95996 841.91998]` (ran). | Ran |
| `PdfOptions.PrintBackground` is `false` by default. The fill color of a `div` with a CSS background is in the inflated page content only with `PrintBackground = true` (ran). | [PdfOptions.cs](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/PdfOptions.cs), ran |
| `chrome-headless-shell-linux64.zip` of 155.0.8059.84 is on Chrome for Testing. | [known-good-versions.json](https://googlechromelabs.github.io/chrome-for-testing/known-good-versions.json) |
| The `ChromeHeadlessShell` cache folder is used only with `HeadlessMode.Shell` and the Chrome browser. The default `HeadlessMode` is `True`, and then the `Chrome` folder is used (checked 2026-10-27). | [Launcher.cs at v25.15.0](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/Launcher.cs), [LaunchOptions.cs](https://github.com/hardkoded/puppeteer-sharp/blob/v25.15.0/lib/PuppeteerSharp/LaunchOptions.cs) |

## Credits

These sources gave ideas. The text is a paraphrase. The plugin `CREDITS.md` lists them.

- [Puppeteer](https://github.com/puppeteer/puppeteer) troubleshooting page and Dockerfile (Apache-2.0): non-root user, fonts, `LANG`, init process.
- [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp) documentation (MIT): the Chrome build per version, the reuse of the browser.
