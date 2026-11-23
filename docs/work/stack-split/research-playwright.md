# Research: playwright (test-stack)

Date: 2026-11-20. Status: research for the new skill `test-stack:playwright`. The skill has guides and references for 3 parts. The parts are Playwright Test in TypeScript, Playwright for .NET (MSTest 4 first, xUnit v3 second), and the Playwright MCP server.

Marks:

- **verified, source**: the linked page, release, issue, package feed, or source file says it.
- **verified, probe**: the model wrote it in the probe of this research (see "Probe method").
- **not verified**: from one weak source, or from memory. Check it before a rule uses it.

No fact in this file comes from a build or a test run. This research did not install Playwright.

## Contents

- Summary of versions
- Playwright Test (TypeScript)
- Playwright for .NET
- Playwright MCP and the Playwright CLI
- Probe method
- Recent mistakes
- New traps for the skill
- Facts that are not verified

## Summary of versions

All rows: verified, source (npm registry, NuGet registration API, GitHub releases), read on 2026-11-20.

| Item | Version | Date | Note |
|------|---------|------|------|
| `@playwright/test`, `playwright` (npm) | 1.65.1 | 2026-11-13 | 1.65.0 on 2026-11-05. `next` is 1.66.0-alpha |
| `Microsoft.Playwright` (NuGet) | 1.65.1 | 2026-11-18 | `netstandard2.0`. Bundles Node.js 26 |
| `Microsoft.Playwright.MSTest` | 1.65.1 | 2026-11-18 | For MSTest 2 and 3. Depends on `MSTest.TestAdapter` and `MSTest.TestFramework` 2.2.7 or later |
| `Microsoft.Playwright.MSTest.v4` | 1.65.1 | 2026-11-18 | For MSTest 4. First stable 1.55.0. Depends on `MSTest.TestFramework` 4.x only |
| `Microsoft.Playwright.Xunit` | 1.65.1 | 2026-11-18 | xUnit v2 (`xunit.extensibility.core` 2.8 or later) |
| `Microsoft.Playwright.Xunit.v3` | 1.65.1 | 2026-11-18 | xUnit v3 (`xunit.v3.extensibility.core` 1.0.1 or later). First stable 1.55.0 |
| `Microsoft.Playwright.NUnit` | 1.65.1 | 2026-11-18 | NUnit 3.13.2 or later |
| `Microsoft.Playwright.CLI` (dotnet tool) | 1.2.3 | 2022-03-16 | Deprecated (Legacy). The message says: use `pwsh bin/Debug/net8.0/playwright.ps1 install` |
| `@playwright/mcp` | 0.0.86 | 2026-11-18 | Still 0.0.x. About one release each 2 weeks |
| `@playwright/cli` (`playwright-cli`) | 0.1.25 | 2026-11-18 | First release 2026-01-26 |
| `@playwright/experimental-ct-react` | 1.62.1 | 2026-07-30 | No more updates since 1.63 (see component testing) |
| MSTest, `MSTest.Sdk` | 4.5.3 | 2026-11-11 | For comparison |
| `xunit.v3` | 4.0.3 | 2026-11-09 | xUnit v3 is now at package version 4.x. It uses `xunit.v3.mtp-v2` |
| `Reqnroll`, `Reqnroll.MSTest` | 3.3.4 | 2026-03-23 | For the later Reqnroll layer only. Not researched |

Browsers in 1.65.1: Chromium 159, Firefox 159, WebKit 27.2. Playwright 1.65 no longer tests on Node.js 20. Use Node.js 22, 24, or 26 (verified, source: [v1.65.0](https://github.com/microsoft/playwright/releases/tag/v1.65.0)).

### Support of .NET versions and test frameworks

- All .NET packages target `netstandard2.0`. `Microsoft.Playwright.NUnit` also has `net462` and `netcoreapp3.1` groups. There is no `net10.0` or `net11.0` asset. (verified, source: NuGet. That they run on .NET 10 and .NET 11: not verified, no build)
- MSTest 4 needs `Microsoft.Playwright.MSTest.v4`. The old package with MSTest 4 gives a compile error about the type `TestContext` in an assembly that the project does not reference. A MSTest maintainer gave the fix. (verified, source: [playwright-dotnet#3265](https://github.com/microsoft/playwright-dotnet/issues/3265))
- The `.v4` package keeps the namespace `Microsoft.Playwright.MSTest` (`RootNamespace` in the project file). Test code does not change, only the package. (verified, source: [Playwright.MSTest.v4.csproj](https://github.com/microsoft/playwright-dotnet/blob/main/src/Playwright.MSTest.v4/Playwright.MSTest.v4.csproj))
- The Playwright docs name only `Microsoft.Playwright.MSTest`. They do not name the `.v4` package. (verified, source: [intro-csharp.md](https://github.com/microsoft/playwright/blob/main/docs/src/intro-csharp.md), [test-runners-csharp.md](https://github.com/microsoft/playwright/blob/main/docs/src/test-runners-csharp.md))
- xUnit v3 uses `Microsoft.Playwright.Xunit.v3`. The template is `dotnet new xunit3`. (verified, source: intro-csharp.md)
- The retry extension of Microsoft.Testing.Platform (MTP) works with MSTest, NUnit, and xUnit v3, not xUnit v2. The major version of the retry package must match the MTP version: xUnit v3 3.x needs 1.x, xUnit v3 4.x needs 2.x. (verified, source: test-runners-csharp.md)

## Playwright Test (TypeScript)

### Current patterns

All from the docs and the release notes 1.59 to 1.65 (verified, source, [releases](https://github.com/microsoft/playwright/releases)):

- **Locators.** `getByRole`, `getByLabel`, `getByText`, `getByTestId` first, and `locator.filter({ hasText })` to select one row. New: `locator.visible()` replaces the CSS `:visible` (1.63). `locator.within(parent)` (1.64). `page.frameLocator()` without a selector finds an element in each frame (1.63), and `getByRole` takes `description` (1.60).
- **Assertions.** Web-first `await expect(locator).toBeVisible()`, `toHaveText`, `toHaveCount`, `toHaveURL`. `expect.soft.poll` (1.61). Most actions and assertions take an `AbortSignal` (1.62).
- **Aria snapshots.** `toMatchAriaSnapshot` on a locator, and on a page since 1.60. `locator.ariaSnapshotJSON()` (1.63). Since 1.65, text that the browser does not render is not in the accessible name.
- **Auth.** A setup project (`testMatch: /.*\.setup\.ts/`) saves `storageState` to `playwright/.auth/user.json`. Each browser project has `dependencies: ['setup']`. Storage state can now include passkeys (`credentials`, 1.62) and OPFS (`opfs`, 1.63). Virtual passkeys: `context.credentials` (1.61). (verified, source: [auth.md](https://github.com/microsoft/playwright/blob/main/docs/src/auth.md))
- **Steps.** `test.step(title, body, { subtitle, params })` (1.63). `test.abort(message)` from a fixture, hook, or route handler (1.60).
- **Parallel runs.** `fullyParallel`, `--shard`. New: test locks, `{ lock: 'name' }` on a test or `test.describe.configure({ lock })`, and `lock: '-exclusive-'` (1.63 to 1.65). `--shuffle [seed]` finds tests that depend on each other (1.64). Since 1.65 a sharded run without tests fails, unless `--pass-with-no-tests`.
- **Retries and flaky tests.** `retries` with `retryStrategy: 'isolated'` (1.62). `--fail-on-flaky-tests`. Trace modes `on-first-retry`, `retain-on-failure-and-retries`. Video has the same modes since 1.61.
- **Projects.** `default: false` keeps a project in the config but out of a plain `npx playwright test` (1.64). Run all with `--project="*"`.
- **webServer.** `command`, `url` or `port` (not both, else the runner stops with an error), `reuseExistingServer: !process.env.CI`. (verified, source: [webServerPlugin.ts](https://github.com/microsoft/playwright/blob/main/packages/playwright/src/plugins/webServerPlugin.ts))
- **Clock.** `page.clock.install()`, `setFixedTime`, `pauseAt`, `runFor`. (verified, source: [clock.md](https://github.com/microsoft/playwright/blob/main/docs/src/clock.md). The API did not change in 1.59 to 1.65)
- **Network.** `page.route`, `route.fulfill`, `routeFromHAR`. HAR recording as a trace API: `context.tracing.startHar()` with `await using` (1.60). HAR and traces include WebSocket traffic (1.61).
- **Visual comparisons.** `toHaveScreenshot()`. WebP snapshots by name `.webp` (1.62) or `expect.toHaveScreenshot.type: 'webp'` (1.64). `--update-snapshots=missing` now passes the run (1.64).
- **Reporters.** New `failures` reporter and `onlyFailures` for HTML, JSON, and JUnit (1.65). `--add-reporter` (1.63). `perfetto` reporter (1.63).
- **Bun.** `bunx --bun playwright test` runs under Bun (1.65). Without `--bun` it runs on Node.js.

### Component testing

Since 1.62, component testing uses a stories and galleries model with a `mount(storyId)` fixture. Since 1.63 the packages `@playwright/experimental-ct-react`, `-react17`, and `-vue` get no updates. The docs have a migration guide. (verified, source: [v1.62.0](https://github.com/microsoft/playwright/releases/tag/v1.62.0), [v1.63.0](https://github.com/microsoft/playwright/releases/tag/v1.63.0), [test-components-js.md](https://github.com/microsoft/playwright/blob/main/docs/src/test-components-js.md))

### Test agents

- `npx playwright init-agents --loop=<claude|codex|copilot|opencode|vscode>` writes 3 agent definitions: planner (writes a Markdown plan in `specs/`), generator (writes tests from the plan), healer (runs and repairs failing tests). Run it again after each Playwright update. The default loop is `copilot`. (verified, source: [test-agents-js.md](https://github.com/microsoft/playwright/blob/main/docs/src/test-agents-js.md), [playwright#43440](https://github.com/microsoft/playwright/issues/43440))
- The healer can mark a test as skipped (or `test.fixme()`) when it decides that the app has a defect. (verified, source: test-agents-js.md)
- A maintainer wrote on 2026-10-30 that an agent that fixes the tests directly is now often better than the healer. (verified, source: [playwright#43675](https://github.com/microsoft/playwright/issues/43675))
- The generator definition of 1.63.0 had a syntax error in its example (`async { page } =>`). Fixed 2026-09-17. (verified, source: [playwright#42743](https://github.com/microsoft/playwright/issues/42743))
- `npm init playwright@latest` (on `main`) writes `.claude/skills/playwright/SKILL.md` with no opt-out flag. (verified, source: [playwright#43958](https://github.com/microsoft/playwright/issues/43958). The state in a released version: not verified)
- `npx playwright test --debug=cli` lets an agent debug a test through `playwright-cli` (1.59). (verified, source: [v1.59.0](https://github.com/microsoft/playwright/releases/tag/v1.59.0))
- The CLI has `--run-agents <missing|all|none>` "to generate the code for `page.perform`". The API docs have no `page.perform`. (verified, source: [test-cli-js.md](https://github.com/microsoft/playwright/blob/main/docs/src/test-cli-js.md). The API and its status: not verified)

### Other agent features

- `page.ariaSnapshot({ mode: 'ai' })` gives refs such as `e2`. `page.getByRef('e2')` turns a ref into a locator (1.64). In .NET 1.65, `NormalizeAsync()` prints the locator as C# code. (verified, source: releases)
- `page.screencast` records a video with chapters and action marks, as a "video receipt" of agent work (1.59). (verified, source: v1.59.0)
- `page.webmcp` tests the WebMCP tools of a page (1.64, experimental browser flag). (verified, source: [v1.64.0](https://github.com/microsoft/playwright/releases/tag/v1.64.0))

### Breaking changes 1.64 and 1.65 that affect existing tests

- Actions wait until the element is not `inert` (1.65).
- Cookie matching follows RFC 6265. Proxy credentials go only to the proxy. Use `httpCredentials` for a site (1.65).
- `params` in `APIRequestContext` adds to the query of the URL, it does not replace it (1.65).
- A class field initializer that reads a constructor parameter property now fails (`useDefineForClassFields`). Page objects with `readonly submit = this.page.getByRole(...)` break. Move the code into the constructor (1.65).
- Device descriptors now set `screen` (1.64). Playwright treats elements in a hidden iframe as hidden (1.64).

## Playwright for .NET

### Current patterns

- **Base classes.** `PageTest`, `ContextTest`, `BrowserTest`, `PlaywrightTest` in the namespace of the adapter. Override `ContextOptions()` for `BaseURL`, viewport, locale. `Expect(...)` comes from the base class, or from `using static Microsoft.Playwright.Assertions;` without a base class. (verified, source: test-runners-csharp.md)
- **MSTest parallel runs.** MSTest runs classes in parallel and the tests in one class one after the other (`ExecutionScope.ClassLevel`). (verified, source: test-runners-csharp.md)
- **Browser install.** Build first, then `pwsh bin/Debug/<tfm>/playwright.ps1 install`. The docs example uses `net8.0`, so change it to `net10.0` or `net11.0`. `install --with-deps` adds the Linux libraries, and `install --no-remove` keeps the browsers of other installs (1.63). Microsoft deprecated the dotnet tool `Microsoft.Playwright.CLI`. (verified, source: intro-csharp.md, NuGet deprecation, [.NET releases](https://github.com/microsoft/playwright-dotnet/releases))
- **Install without PowerShell.** `Microsoft.Playwright.Program.Main(["install", "chromium"])` in code. (not verified in the current docs. Haiku used it in probe p5)
- **Linux.** Node.js 26 in the .NET package needs `libatomic1` (1.65). (verified, source: [.NET v1.65.0](https://github.com/microsoft/playwright-dotnet/releases/tag/v1.65.0))
- **Timeouts.** `ActionTimeout` and `NavigationTimeout` run settings for each base class (1.65). `PlaywrightAssertionException` for a failed `Expect` (1.65). (verified, source: .NET v1.65.0)
- **Retries.** Playwright for .NET does not retry. Use `[Retry]` (MSTest, NUnit) or the MTP retry extension. Each attempt gets a new context and page. (verified, source: test-runners-csharp.md)

### Configuration: .runsettings and MTP

- The `<Playwright>` section of `.runsettings` (`BrowserName`, `LaunchOptions`, `ExpectTimeout`, and since 1.65 `ActionTimeout`) goes through a VSTest settings provider. MTP does not call it. On MTP, Playwright ignores the section and nothing reports it. (verified, source: [playwright-dotnet#3124](https://github.com/microsoft/playwright-dotnet/issues/3124), open since 2025-03, and [#3375](https://github.com/microsoft/playwright-dotnet/issues/3375))
- `MSTest.Sdk` uses MTP by default. `UseVSTest` still selects VSTest. xUnit v3 runs on MTP. (verified, source: #3124, #3375)
- The command line `dotnet test -- Playwright.BrowserName=chromium` in the docs is the VSTest form. (verified, source: test-runners-csharp.md)
- On MTP 2.3 or later, the `environmentVariables` section of `testconfig.json` can set `BROWSER`, `HEADED`, and `PWDEBUG`. It cannot set launch options, context options, or assertion timeouts. (verified, source: comment of 2026-10-21 in #3375. Not run)
- An opt-in package `Microsoft.Playwright.Testing.Platform` that reads a `playwright` section from `testconfig.json` is an open pull request. It is not on NuGet. (verified, source: [playwright-dotnet#3377](https://github.com/microsoft/playwright-dotnet/pull/3377), NuGet 404 on 2026-11-20)
- Visual Studio Test Explorer can also ignore `LaunchOptions` from `.runsettings`. (verified, source: [playwright-dotnet#3287](https://github.com/microsoft/playwright-dotnet/issues/3287), open)

### Start the app under test

- A browser cannot reach the in-memory `TestServer`. Since .NET 10, `WebApplicationFactory<T>` has `UseKestrel()`, `UseKestrel(int port)` (0 for a free port), `UseKestrel(Action<KestrelServerOptions>)`, and `StartServer()`. Call `UseKestrel` before `CreateClient()` or `StartServer()`. (verified, source: [UseKestrel](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.usekestrel?view=aspnetcore-10.0), [StartServer](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.startserver?view=aspnetcore-11.0))
- How a test reads the Kestrel address after `StartServer()` (`ClientOptions.BaseAddress` or `IServerAddressesFeature`): not verified.
- Since .NET 10 a source generator makes `public partial class Program`. An analyzer tells you to remove the explicit declaration. (verified, source: [What's new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0#miscellaneous))
- Aspire: start the AppHost with `Aspire.Hosting.Testing` and give its endpoint to Playwright. The call `app.GetEndpoint("web")`: not verified in this round. The `aspire` skill owns the AppHost side.
- Microsoft Playwright Testing (the cloud service) retired on 2026-03-08. The new service is Playwright Workspaces in Azure App Testing (`npm init @azure/playwright@latest`). (verified, source: [Learn](https://learn.microsoft.com/rest/api/playwright/))

### Integration points for Reqnroll (later)

These are the places where a Reqnroll layer attaches. This research did not read the Reqnroll docs.

- Reqnroll step classes cannot inherit from `PageTest`. They need the browser and page from a hook and the DI container of the scenario. (not verified)
- Each scenario needs its own `IBrowserContext`, the same rule as each test in the base classes.
- Trace and screenshot on failure must go in an `[AfterScenario]` hook, not in the Playwright base class.
- The run settings problem on MTP applies to Reqnroll on MSTest 4 too.

## Playwright MCP and the Playwright CLI

### Install for Claude Code

- `claude mcp add playwright npx @playwright/mcp@latest`. (verified, source: [README](https://github.com/microsoft/playwright-mcp/blob/main/README.md))
- Playwright 1.62 also bundles the server and the CLI: `npx playwright mcp` and `npx playwright cli`. (verified, source: v1.62.0)
- The MCP README recommends the CLI with its skills for a coding agent. CLI calls use fewer tokens. MCP is for long exploratory loops with a persistent browser. (verified, source: README)
- `playwright-cli`: `npm install -g @playwright/cli@latest`, then `playwright-cli install --skills`. Headless by default. The MCP server opens a visible browser by default. (verified, source: [playwright-cli README](https://github.com/microsoft/playwright-cli))

### Key options

All verified, source: README options table (0.0.86).

| Option | Effect |
|--------|--------|
| `--headless` | Headless. Without it, the browser is visible |
| `--isolated` | Profile in memory. Without it, the server keeps a persistent profile on disk. One profile can serve one browser only, so parallel clients need `--isolated` or their own `--user-data-dir` |
| `--storage-state <path>` | Load cookies and local storage into an isolated context |
| `--user-data-dir <path>` | Persistent profile in a known folder |
| `--caps <list>` | Opt-in tool groups: `vision`, `pdf`, `devtools`, `network`, `storage`, `config`, `testing` |
| `--allowed-origins`, `--blocked-origins` | Origin filter. "Does not serve as a security boundary and does not affect redirects" |
| `--allowed-hosts` | Hosts that the HTTP server answers. Default: localhost and loopback |
| `--secrets <path>` | Dotenv file. The server replaces these values in tool output. "A convenience and not a security feature" |
| `--redact-url-params <names>` | Replaces runtime tokens in URLs in the output (0.0.84) |
| `--codegen csharp` | Code in tool output in C#. Default TypeScript |
| `--snapshot-mode none`, `--image-responses omit` | Fewer tokens |
| `--allow-unrestricted-file-access` | Off by default: file access stays in the workspace roots |
| `--extension` | Connect to the user's Chrome or Edge with the Playwright extension |
| `--no-webmcp` | Hide the `webmcp_<tool>` tools of a page |
| `--idle-timeout` | Closes an idle headless browser. Default one hour |

### Tools

- Core: `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_type`, `browser_fill_form`, `browser_find`, `browser_wait_for`, `browser_evaluate`, `browser_take_screenshot`, `browser_console_messages`, `browser_network_requests`, `browser_tabs`, and others. (verified, source: README)
- `browser_run_code_unsafe` "executes arbitrary JavaScript in the Playwright server process and is RCE-equivalent". (verified, source: README)
- `--caps=testing` adds `browser_generate_locator` and `browser_verify_*`. `--caps=vision` adds `browser_mouse_*_xy` for coordinates. Snapshot mode (the accessibility tree) is the default. Vision mode is only for elements that the tree does not show. (verified, source: README)
- `browser_tabs` with `isolatedContext: "<name>"` gives separate sessions in one browser (0.0.84). (verified, source: [releases](https://github.com/microsoft/playwright-mcp/releases))

### Security

- "Playwright MCP is **not** a security boundary." The MCP client must decide which actions an agent can do. (verified, source: README, [playwright-mcp#1758](https://github.com/microsoft/playwright-mcp/issues/1758))
- Agents go to guessed domains. For a company that the model did not know, it called `browser_navigate` in 96 to 100 percent of trials (Claude Sonnet 4.6 included). The maintainers say the URL choice belongs to the model and the client. (verified, source: [playwright-mcp#1783](https://github.com/microsoft/playwright-mcp/issues/1783))
- The README listed wrong environment variables, for example `PLAYWRIGHT_MCP_NO_SANDBOX`. The correct form is `PLAYWRIGHT_MCP_SANDBOX=false`. Fixed 2026-11-17. (verified, source: [playwright-mcp#1861](https://github.com/microsoft/playwright-mcp/issues/1861))
- Relative paths in a `--config` file resolved against the working folder of the process, not the config file. Fixed 2026-11-07. Use absolute paths in older versions. (verified, source: [playwright-mcp#1836](https://github.com/microsoft/playwright-mcp/issues/1836))
- On a page that cannot run timers (CSP sandbox, paused clock), tools hung with no timeout. Fixed 2026-11-03. (verified, source: [playwright#43752](https://github.com/microsoft/playwright/issues/43752))

### When to use which

| Task | Use |
|------|-----|
| A check that must run again in CI | A Playwright test |
| A coding agent looks at a page, takes a screenshot, finds a locator | `playwright-cli` with its skill (fewer tokens) |
| A long exploratory session, or an MCP client without a shell | Playwright MCP |
| A new test from a walk through the app | MCP or CLI to explore, then write a normal test with role locators. `--codegen` gives a draft only |

## Probe method

- Tool: `claude -p` (headless) 2.1.327, one call at a time, from an empty scratch folder, stdin closed.
- The research removed the variables of the parent session (`CLAUDECODE`, each `CLAUDE_*` variable).
- Flags: `--tools ""`, `--disallowedTools` (Skill, Agent, Bash, Read, Edit, Write, WebFetch, WebSearch, Glob, Grep), `--strict-mcp-config`, `--disable-slash-commands`, `--setting-sources ""`, `--no-session-persistence`.
- Models: `haiku` (Haiku 5.5) and `sonnet` (Sonnet 5.5). 5 prompts for each model. Each prompt starts with "Today is 2026-11-20. You have no tools", and asks for code only and the latest stable versions.
- Prompts: p1 TypeScript login and order list test with config. p2 the same in .NET with MSTest 4, with the `.csproj` and the browser install. p3 Playwright MCP for Claude Code for a local app with a login. p4 TypeScript: log in one time, start the app with `npm run start`. p5 .NET xUnit v3 with `WebApplicationFactory` and a real browser.
- Total cost: $0.11 (10 calls, no repeat).

## Recent mistakes

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | p1 | Current. Role locators, web-first assertions, `trace: 'on-first-retry'`. No mistake found |
| Sonnet 5.5 | p1 | Current. `getByTestId`, `toHaveCount`, `toHaveText`. No mistake found |
| Haiku 5.5 | p2 | `MSTest` 4.0.2 with `Microsoft.Playwright.MSTest` 1.56.0, not `.v4` (compile error, #3265). Deprecated `dotnet tool install --global Microsoft.Playwright.CLI`. CSS locators `table#orders tbody tr`, `td.status` |
| Sonnet 5.5 | p2 | Said it cannot confirm MSTest 4 support, so it wrote a manual browser lifecycle and no adapter. `Version="4.*"` and `Version="*"`. No `MSTest.Sdk`. CSS locator `table tbody tr` |
| Haiku 5.5 | p3 | Says `--allowed-origins` keeps the agent from other sites. The README says it is not a security boundary. Unpinned `@latest` |
| Sonnet 5.5 | p3 | Current. `--secrets`, says `--allowed-origins` is "not a security boundary". Unpinned `@latest`. Neither model named `playwright-cli` |
| Haiku 5.5 | p4 | `webServer` with both `url` and `port`: the runner throws at start. Otherwise current (setup project, `storageState`, `dependencies`) |
| Sonnet 5.5 | p4 | Current. No mistake found |
| Haiku 5.5 | p5 | Own `CreateHost` override with `UseKestrel().UseUrls()` and a free-port hack, not `UseKestrel(0)`. `Microsoft.Playwright` 1.56.0, `xunit.v3` 3.2.0, `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` (VSTest). `public partial class Program` |
| Sonnet 5.5 | p5 | Builds 2 hosts in `CreateHost` (TestServer and Kestrel), not `UseKestrel`. `public partial class Program` |

Summary: the TypeScript answers are mostly current. The .NET answers use versions that are 9 to 10 minor versions old, and the wrong MSTest 4 adapter or no adapter. They use CSS locators and do not know `WebApplicationFactory.UseKestrel`. Both models used `net10.0`.

### Public reports since 2026-05

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| CSS or XPath locators and `waitForTimeout(3000)`. Tests that depend on each other, and data made through the UI. `data-testid` on each element, `if (await locator.isVisible())`, checks on CSS classes, one browser only | AI agents in general | 2026-06-20 | [dev.to](https://dev.to/aiwithanton/playwright-best-practices-10-rules-ai-agents-get-wrong-2026-5cii) | not verified (no named model, no run data) |
| Claude Code suite: `getByTestId('todo-item')` matched 285 elements from earlier runs (strict mode). `.first()` as the fix took the wrong item. `waitForTimeout(500)` between steps. 40 of 70 tests passed on the first run | Claude Code | Updated 2026-09-20 | [momentic.ai](https://momentic.ai/blog/what-tests-look-like-with-claude-code) | not verified (vendor post) |
| Generated test checks only the success text, not the email, the reset link, or token reuse. A real bug stayed hidden | Model not named | 2026-07-14 | [dev.to](https://dev.to/jeffthoensen/my-ai-generated-tests-kept-passing-for-the-wrong-reason-26ml) | not verified |
| Healer agent asks the user questions, although its definition says not to | Claude Code with Opus, Playwright 1.58.1 | 2026-07-23 | [playwright#41954](https://github.com/microsoft/playwright/issues/41954) | verified, source |
| Generator agent definition has invalid TypeScript in its example | Playwright 1.63.0 agent files | 2026-09-16 | [playwright#42743](https://github.com/microsoft/playwright/issues/42743) | verified, source |
| Agents navigate to guessed domains through MCP | Claude Sonnet 4.6 and another model, `@playwright/mcp` 0.0.83 | 2026-10-01 | [playwright-mcp#1783](https://github.com/microsoft/playwright-mcp/issues/1783) | verified, source |

No GitHub issue in microsoft/playwright-dotnet after 2026-05-20 reports an AI mistake. The searches used "Claude", "Copilot", "LLM", "AI generated", and "hallucinat".

## New traps for the skill

| Trap | Current form | Mark | Source |
|------|--------------|------|--------|
| `Microsoft.Playwright.MSTest` with MSTest 4 | `Microsoft.Playwright.MSTest.v4` (same namespace) | verified, source and probe | playwright-dotnet#3265, probe p2 |
| No adapter and a manual browser lifecycle "because MSTest 4 support is unknown" | `PageTest` from the `.v4` package | verified, probe | probe p2 |
| `dotnet tool install --global Microsoft.Playwright.CLI` | `pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium` after `dotnet build` | verified, source and probe | NuGet deprecation, probe p2 |
| `bin/Debug/net8.0/playwright.ps1` copied from the docs | The TFM of the project (`net10.0`, `net11.0`) | verified, source | intro-csharp.md |
| `<Playwright>` in `.runsettings` on MSTest.Sdk or xUnit v3 (MTP): ignored, no warning | Environment variables (`BROWSER`, `HEADED`) or `testconfig.json` `environmentVariables` on MTP 2.3+. Or `ContextOptions()` in code | verified, source | playwright-dotnet#3124, #3375 |
| `dotnet test -- Playwright.BrowserName=...` on MTP | VSTest syntax only. Use environment variables on MTP | verified, source | test-runners-csharp.md, #3124 |
| Own `CreateHost` override or 2 hosts for a real browser | `factory.UseKestrel(0)` and `factory.StartServer()` (.NET 10 and 11) | verified, source and probe | Learn API, probe p5 |
| `public partial class Program { }` in the app | Remove it on .NET 10 and later: a source generator makes it | verified, source and probe | ASP.NET Core 10 notes, probe p5 |
| `Microsoft.Playwright` 1.56, `xunit.v3` 3.x, `Version="*"` | Pin 1.65.1 (Playwright) and 4.0.3 (xunit.v3) in `Directory.Packages.props` | verified, probe | probes p2, p5 |
| xUnit v3 with `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` | xUnit v3 4.x on MTP (`xunit.v3` brings `xunit.v3.mtp-v2`) | verified, source and probe | NuGet, probe p5 |
| CSS locators (`table tbody tr`, `td.status`) | `GetByRole(AriaRole.Row)`, `Filter(new() { HasText })` | verified, probe | probe p2 |
| `waitForTimeout` or `Task.Delay` between steps | Web-first `expect` / `Expect(...)` assertions | not verified (blogs only) | dev.to, momentic.ai |
| `.first()` on a list that keeps data from earlier runs | `filter({ hasText: uniqueValue })` and own test data per test | not verified (vendor post) | momentic.ai |
| `webServer` with both `url` and `port` | Only `url` (preferred) or only `port` | verified, source and probe | webServerPlugin.ts, probe p4 |
| Page object field `readonly x = this.page.getByRole(...)` | Set the field in the constructor (1.65, `useDefineForClassFields`) | verified, source | v1.65.0 |
| `page.locator('button:visible')` | `page.locator('button').visible()` | verified, source | v1.63.0 |
| `@playwright/experimental-ct-react` for new component tests | Stories and galleries with `mount(storyId)` | verified, source | v1.62.0, v1.63.0 |
| Healer agent as the fix for each red test | Classify first: a real regression stays red. Do not accept a `test.fixme()` or skip from the healer without review | verified, source | test-agents-js.md, playwright#43675 |
| `--allowed-origins` as a security control | Not a boundary, does not cover redirects. Use test accounts and client permissions | verified, source and probe | README, probe p3 |
| `--secrets` as protection of passwords | Only text replacement in output. Use test accounts | verified, source | README |
| Playwright MCP for each browser look in a coding agent | `playwright-cli` with its skill, MCP for long exploration | verified, source | README |
| `npx @playwright/mcp@latest` in a shared `.mcp.json` | Pin the version (0.0.x changes each 2 weeks) | not verified (practice, no incident) | npm release dates |
| `browser_run_code_unsafe` enabled for an agent on untrusted pages | It is RCE-equivalent. Keep the agent on trusted local apps | verified, source | README |
| Node.js 20 for Playwright 1.65 | Node.js 22, 24, or 26 | verified, source | v1.65.0 |
| `@azure/microsoft-playwright-testing` (Microsoft Playwright Testing) | Retired 2026-03-08. Playwright Workspaces (`@azure/playwright`) | verified, source | Learn |

## Facts that are not verified

- That the `netstandard2.0` .NET packages work on .NET 11. No build ran.
- That `Microsoft.Playwright.Xunit.v3` 1.65.1 (minimum `xunit.v3.extensibility.core` 1.0.1) works with `xunit.v3` 4.0.3.
- That `Microsoft.Playwright.MSTest.v4` works with `MSTest.Sdk` 4.5.3 and MTP without extra properties.
- How a test reads the Kestrel address after `WebApplicationFactory.StartServer()`.
- The Aspire call `app.GetEndpoint(...)` for a Playwright test in Aspire 17.
- `Microsoft.Playwright.Program.Main(["install"])` as a supported install path. The current docs do not name it.
- `page.perform` and `--run-agents`. The CLI docs name them, the API docs do not.
- Whether a released `create-playwright` writes `.claude/skills/playwright/SKILL.md` (seen on `main`).
- The Reqnroll integration points. This research did not read the Reqnroll docs.
- The blog reports (dev.to, momentic.ai). They name no model version and give no run data.
- No public report after 2026-05-20 names an AI mistake with Playwright for .NET. The .NET traps come from the probe only.
