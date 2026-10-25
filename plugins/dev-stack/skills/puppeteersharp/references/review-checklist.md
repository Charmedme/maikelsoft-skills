# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Sandbox and image
- Browser lifetime
- Untrusted content
- PDF and waits
- Tests
- Decisions

## Sandbox and image

| # | Check | Type |
|---|-------|------|
| 1 | No `--no-sandbox`, `--disable-setuid-sandbox`, or `PUPPETEER_DANGEROUS_NO_SANDBOX`, unless an ADR allows it for trusted templates (SEC-24). | tool (PPTR-NO-SANDBOX) |
| 2 | The final image runs as a non-root user (`USER $APP_UID`). | tool (hadolint DL3002), judge |
| 3 | The container gets a seccomp profile that allows `clone`, `clone3`, `unshare`. No `SYS_ADMIN`, no `privileged`, no `seccomp=unconfined`. | judge |
| 4 | `chrome-headless-shell` is installed at build time and pinned to `Chrome.DefaultBuildId`. No download at runtime. | tool (PPTR-RUNTIME-DOWNLOAD), judge |
| 5 | The renderer image is full `aspnet:10.0`, with the Chrome libraries, fonts, `LANG`, and `tini` (or `--init`). | judge |
| 6 | The renderer has no secrets it does not need, and no egress when assets are inline. | judge |

## Browser lifetime

| # | Check | Type |
|---|-------|------|
| 7 | One browser per process: a singleton that launches again after a disconnect and closes at shutdown. | tool (PPTR-BROWSER-PER-CALL), judge |
| 8 | One context per job and one page per render, with `await using`. | judge |
| 9 | Concurrent renders are limited (`SemaphoreSlim`), and the limit comes from configuration. | judge |
| 10 | Headless in all non-local settings. No `Devtools`. | tool (PPTR-HEADFUL) |

## Untrusted content

| # | Check | Type |
|---|-------|------|
| 11 | Each user value is HTML-encoded. No `MarkupString` or concatenation of user text. | judge |
| 12 | The network of the page is blocked (`Allowlist` or request interception) when the HTML holds user text. | judge |
| 13 | CSS, fonts, and images (the logo) are inline or local. | judge |
| 14 | JavaScript is off when the document does not need it. A CSP `<meta>` tag is present. | judge |
| 15 | No request URL reaches `GoToAsync`. A URL source has an `https` host allow list and authorization. | judge |

## PDF and waits

| # | Check | Type |
|---|-------|------|
| 16 | `PdfDataAsync` or `PdfStreamAsync`. No temp file. | tool (PPTR-PDF-FILE) |
| 17 | `SetContentAsync` uses `SetContentOptions` with a timeout. No `NavigationOptions` with network idle. | tool (CS0618), judge |
| 18 | `PdfOptions.Timeout` and the wait timeouts are set. The `CancellationToken` reaches the semaphore wait. | judge |
| 19 | No `WaitForTimeoutAsync`, no fixed delays: a real wait condition. | tool (CS1061), judge |
| 20 | Each `PdfOptions` sets `Format` (or `Width` and `Height`), also in existing code that the change touches. Without it, the PDF is Letter. A CSS `@page` size counts only with `PreferCSSPageSize = true`. | tool (PPTR-PDF-FORMAT), judge |
| 21 | `PrintBackground = true` when the template has a background color or image (the default is `false`). | tool (PPTR-PDF-BACKGROUND), judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 22 | An integration test with the real `chrome-headless-shell` checks the PDF header. | judge |
| 23 | A test proves that user markup makes no request (local listener). | judge |
| 24 | CI does not add `--no-sandbox` to the app. A root-only wrapper is in the CI setup, not in the app. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 25 | SEC-24 is accepted in `docs/dev-team/decisions.md`, or the report marks it as assumed. A sandbox opt-out has an ADR. | judge |
| 26 | The renderer image follows OPS-03 (own image on full `aspnet:10.0`), or the report names the deviation. | judge |
