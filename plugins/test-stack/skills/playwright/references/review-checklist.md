# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Packages and setup
- Locators and assertions
- Test data and sign-in
- App under test
- Agent browser
- Test agents
- Severity
- Do not flag

## Packages and setup

| # | Check | Type |
|---|-------|------|
| 1 | MSTest 4 uses `Microsoft.Playwright.MSTest.v4`, not `Microsoft.Playwright.MSTest`. | tool (PW-MSTEST-ADAPTER) |
| 2 | xUnit v3 uses `Microsoft.Playwright.Xunit.v3`, and no `xunit.runner.visualstudio` or `Microsoft.NET.Test.Sdk`. | judge |
| 3 | Each `Microsoft.Playwright*` reference has one pinned version. No `*` or a floating range. | judge |
| 4 | The browser install uses `playwright.ps1` with the target framework of the project. No `Microsoft.Playwright.CLI` tool. | tool (PW-DOTNET-CLI-TOOL), judge |
| 5 | No `<Playwright>` section in `.runsettings` of a project on Microsoft.Testing.Platform. | tool (PW-RUNSETTINGS) |
| 6 | `webServer` has `url` or `port`, not both. | judge |
| 7 | Node.js 22 or later in CI. | judge |
| 8 | `playwright/.auth/` is in `.gitignore`. | judge |

## Locators and assertions

| # | Check | Type |
|---|-------|------|
| 9 | Locators use role, label, or text. `getByTestId` only where these do not work. No CSS chains or XPath. | judge |
| 10 | No CSS `:visible` in a selector. | tool (PW-CSS-VISIBLE) |
| 11 | Each check is a web-first assertion. No `isVisible()` or `textContent()` as a check. | judge |
| 12 | No fixed wait between steps. | tool (PW-FIXED-WAIT) |
| 13 | No `.first()` or `.nth()` to fix a strict-mode error on a list. A filter on a unique value. | judge |
| 14 | The constructor of a page object sets its locator fields (1.65). | judge |

## Test data and sign-in

| # | Check | Type |
|---|-------|------|
| 15 | Each test makes its own data with a unique value. No test depends on another test. | judge |
| 16 | The test asserts the outcome (the changed data), not only a success message. | judge |
| 17 | Sign-in runs once in a setup project with `storageState` (PW-05). | judge |
| 18 | Passwords come from environment variables of a test account. No password in the code. | judge |
| 19 | A route mock is only for a state that real data cannot make. Each flow has one test against the real API. | judge |

## App under test

| # | Check | Type |
|---|-------|------|
| 20 | A .NET test starts the app with `UseKestrel(0)` and `StartServer()`. No own host in `CreateHost` (PW-03). | judge |
| 21 | No `public partial class Program { }` in an app on .NET 10 or later. | judge |
| 22 | The server starts once for the assembly, not for each test. | judge |

## Agent browser

| # | Check | Type |
|---|-------|------|
| 23 | `@playwright/mcp` and `@playwright/cli` have a pinned version in each shared file. | tool (PW-AGENT-UNPINNED) |
| 24 | No text, comment, or doc treats `--allowed-origins` or `--secrets` as a security control. | judge |
| 25 | `browser_run_code_unsafe` is not allowed for an agent on a page that the team does not control. | judge |
| 26 | The agent uses a test account and a local or test URL. | judge |
| 27 | No `--allow-unrestricted-file-access` without a reason. | judge |

## Test agents

| # | Check | Type |
|---|-------|------|
| 28 | Each red test has a class (app defect, test defect, flaky) before a change (PW-04). | judge |
| 29 | No `test.skip()` or `test.fixme()` from the healer without a human review and a reason. | judge |
| 30 | Generated tests follow rules 1 to 7 of `SKILL.md`. | judge |

## Severity

These findings are blocking:

- A real account, a production password, or a state file in Git (18, 8).
- A skip or `fixme` that hides a real regression (29).
- `browser_run_code_unsafe` for an agent on pages that the team does not control (25).
- `Microsoft.Playwright.MSTest` with MSTest 4: the build fails (1).

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `getByTestId` on an element that has no role and no visible name, for example a chart total.
- `.first()` on a list that the test made and that has one known order, for example "the newest order" after a sort.
- `retries: 2` in CI with a trace on the first retry, when the report names each flaky test (PW-06).
- `Microsoft.Playwright.MSTest` in a project on MSTest 3. Only MSTest 4 needs `.v4`.
- `page.route(...)` for a server error or a slow response that the real backend cannot make on demand.
- A Playwright MCP setup without `--allowed-origins`. The option is not a security control, so its absence is not a finding.
