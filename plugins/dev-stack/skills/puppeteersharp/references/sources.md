# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| PuppeteerSharp, PuppeteerSharp.Cdp | 25.14.0 (MIT, net8.0, net10.0, netstandard2.0) | [nuget.org PuppeteerSharp](https://www.nuget.org/packages/PuppeteerSharp) |
| Chrome build of PuppeteerSharp 25.14.0 | 155.0.8059.79 (`Chrome.DefaultBuildId`) | [BrowserData/Chrome.cs at v25.14.0](https://github.com/hardkoded/puppeteer-sharp/blob/v25.14.0/lib/PuppeteerSharp/BrowserData/Chrome.cs) |
| chrome-headless-shell linux64 | 155.0.8059.79 | [Chrome for Testing](https://googlechromelabs.github.io/chrome-for-testing/) |
| Microsoft.Playwright (alternative) | 1.63.0 (MIT) | [nuget.org](https://www.nuget.org/packages/Microsoft.Playwright) |
| .NET runtime images | `mcr.microsoft.com/dotnet/aspnet:10.0` (Ubuntu 24.04), user `app` 1654 | [.NET container images](https://learn.microsoft.com/dotnet/core/docker/container-images) |

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
- Docker image (runtime stage of `container.md`, Chrome copied from a local zip because the build container had no access to the download host): as user `app` with the default seccomp profile, "No usable sandbox!". With the default profile plus `clone`, `clone3`, `unshare`, a PDF. With `clone`, `clone3` only, "No usable sandbox!" (ran).

Not checked: the full `docker build` with the `curl` download stage (the build container could not verify TLS through the proxy of the lab), the Kubernetes `RuntimeDefault` profile, and AppArmor hosts.

## Credits

These sources gave ideas. The text is a paraphrase. The plugin `CREDITS.md` lists them.

- [Puppeteer](https://github.com/puppeteer/puppeteer) troubleshooting page and Dockerfile (Apache-2.0): non-root user, fonts, `LANG`, init process.
- [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp) documentation (MIT): the Chrome build per version, the reuse of the browser.
