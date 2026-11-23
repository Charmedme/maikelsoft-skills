---
name: playwright
description: End-to-end browser tests with Playwright 1.65, and browser checks by a coding agent. Routes to 3 guides. TypeScript - Playwright Test with webServer, a setup project with storageState, fixtures, role locators, web-first assertions, aria snapshots, clock, route mocks, traces, CI, and test agents. .NET - Microsoft.Playwright with MSTest 4 (.MSTest.v4, PageTest) or xUnit v3 (.Xunit.v3), playwright.ps1, settings on Microsoft.Testing.Platform, and the app under test with WebApplicationFactory UseKestrel. Agent - Playwright MCP or playwright-cli, pinned, with the security limits. Use when code or a review touches playwright.config.ts, @playwright/test, e2e spec files, Microsoft.Playwright packages, PageTest, playwright.ps1, @playwright/mcp, @playwright/cli, or Playwright in .mcp.json. Not for Gherkin features and step bindings (test-stack:reqnroll), .NET unit and API tests (test-stack:dotnet-testing), Angular component tests (frontend-stack:angular), or HTML to PDF (dev-stack:puppeteersharp).
metadata:
  verified: "2026-11-20"
  versions: "Playwright 1.65.1 (npm @playwright/test and NuGet Microsoft.Playwright), Microsoft.Playwright.MSTest.v4 1.65.1, Microsoft.Playwright.Xunit.v3 1.65.1, MSTest 4.5.3, xunit.v3 4.0.3, @playwright/mcp 0.0.86, @playwright/cli 0.1.25, Node.js 22 or later, .NET 10 and .NET 11"
---

# Playwright

Rules for end-to-end tests with Playwright, and for a coding agent that looks at a page through a browser. A test that must run again in CI is a Playwright test. A look by an agent that nobody runs again goes through `playwright-cli` or Playwright MCP.

Other skills own the parts around the browser:

- Gherkin features and step bindings, also with Playwright in the steps: `test-stack:reqnroll`.
- .NET unit tests, API tests, `WebApplicationFactory` with `HttpClient`, and Testcontainers: `test-stack:dotnet-testing`.
- Angular component tests and the Angular code: `frontend-stack:angular`.
- An Aspire AppHost that a test starts: `dev-stack:aspire`.
- Secrets and test accounts in general: `dev-stack:secure-coding`.

## First

1. Read `references/stack-rules.md`. It has the common rules of all stack skills.
2. Find the part of the task. Done when you can name the guide:

   | Signal | Guide |
   |--------|-------|
   | `playwright.config.ts`, `@playwright/test` in `package.json`, `*.spec.ts` with `@playwright/test` | `references/typescript.md` |
   | `Microsoft.Playwright*` in `*.csproj` or `Directory.Packages.props`, `PageTest`, `playwright.ps1` | `references/dotnet.md` |
   | `@playwright/mcp`, `@playwright/cli`, `playwright-cli`, Playwright in `.mcp.json`, or a task "look at the page", "take a screenshot", "check it in the browser" | `references/agent-browser.md` |

3. Find the versions. Done when you can name each one that the task touches:
   - Playwright: `@playwright/test` in `package.json`, or `Microsoft.Playwright*` in the .NET project. 1.65 is current. For an older version, read "Version gate".
   - .NET test framework: `MSTest.Sdk` or `MSTest*` 4.x, or `xunit.v3` 4.x. MSTest 4 needs the `.v4` adapter (rule 10).
   - Node.js: 22, 24, or 26. Playwright 1.65 does not test on Node.js 20.
   - MCP server or CLI: the version in `.mcp.json` or the install command.
4. Find the app under test: the start command and its URL (TypeScript), or the project that the test starts (.NET). Done when you know how the test gets a running app.

## Version gate

Existing code keeps its Playwright version until the task asks for an upgrade (stack rule 3). On an upgrade to 1.65, check these breaks first. Each one fails at run time, not at compile time:

- Page objects with a field initializer that reads a constructor parameter property (`readonly submit = this.page.getByRole(...)`) fail. Set the field in the constructor.
- Actions wait until the element is not `inert`.
- `params` in `APIRequestContext` adds to the query of the URL. It does not replace it.
- Cookie matching follows RFC 6265. Proxy credentials go only to the proxy. Use `httpCredentials` for a site.
- Since 1.64: device descriptors set `screen`, and Playwright treats elements in a hidden iframe as hidden.
- Since 1.65: a sharded run that has no tests fails, unless `--pass-with-no-tests`.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The owner approved PW-01 to PW-04 as catalog defaults. The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Browser look by a coding agent | `playwright-cli` for a short check in a new setup. A project with Playwright MCP keeps it. MCP for long exploration | PW-01 |
| .NET adapter | `Microsoft.Playwright.MSTest.v4` with `PageTest` (MSTest 4). `Microsoft.Playwright.Xunit.v3` (xUnit v3) | PW-02 |
| App under test (.NET) | `WebApplicationFactory` with `UseKestrel(0)` and `StartServer()`. Aspire AppHost: `Aspire.Hosting.Testing`. Deployed URL: smoke tests only | PW-03 |
| Test agents (planner, generator, healer) | Not by default. Classify each red test first. No skip from the healer without review | PW-04 |
| Sign-in | A setup project saves `storageState` once. One file for each role | PW-05 |
| Browsers and retries in CI | Chromium on each pull request. 2 retries with a trace on the first retry. A flaky test is a defect | PW-06 |

## Rules

### All languages

1. Find elements by role, label, or text: `getByRole`, `getByLabel`, `getByText` (.NET: `GetByRole`, `GetByLabel`, `GetByText`). Use `getByTestId` when the element has no role or name. CSS and XPath selectors break on each layout change, and they test nothing that a user sees. To select one row, filter on a value that only this test made: `getByRole('row').filter({ hasText: orderNumber })`.
2. Assert with web-first assertions: `await expect(locator).toBeVisible()`, `toHaveText`, `toHaveCount`, `toHaveURL` (.NET: `await Expect(locator).ToBeVisibleAsync()`). They wait and retry until the timeout. A check on `isVisible()` or a read of `textContent()` does not wait, so the test is flaky.
3. Do not wait for a fixed time: no `waitForTimeout`, `WaitForTimeoutAsync`, `Task.Delay`, or `setTimeout` between steps. A fixed wait is too short on a slow CI machine and too long on a fast one. Wait for the result that the user sees (rule 2). For time in the app, use the clock (`page.clock`).
4. Make each test independent. Each test makes its own data, through the API or the database, not through the UI of an earlier test. Then the tests can run in parallel, in each order, and with `--shuffle`.
5. Sign in once in a setup step and reuse the saved `storageState` (PW-05). Keep `playwright/.auth/` out of Git. Use test accounts with test data only, never a real account. The state file holds live session cookies.
6. Assert the outcome, not only the success message. Check the data that the action changed, for example the row in the list or the new status.
7. Record a trace on the first retry. A trace shows each action, the DOM, the network, and the console of a failed test.

### TypeScript

8. Read `references/typescript.md` before you write a config, a fixture, a page object, or a CI job. The main traps:
   - `webServer` takes `url` or `port`, not both. With both, the runner stops at start with an error. Use `url`.
   - A page object sets its locator fields in the constructor (1.65).
   - Use `locator.visible()`, not the CSS `:visible` in a selector (1.63).
   - Do not start new component tests on `@playwright/experimental-ct-react` or `-vue`. They get no updates since 1.63.
9. Use Node.js 22 or later in CI and in the local setup.

### .NET

10. With MSTest 4, reference `Microsoft.Playwright.MSTest.v4`. The package `Microsoft.Playwright.MSTest` is for the older MSTest versions (2 and 3). With MSTest 4 the build passes with warnings MSTEST0082, and then the run finds 0 tests. A green build does not prove the package. The `.v4` package keeps the namespace `Microsoft.Playwright.MSTest`, so the test code does not change. The Playwright docs name only the old package. With xUnit v3, reference `Microsoft.Playwright.Xunit.v3`.
11. Use the base classes `PageTest`, `ContextTest`, or `BrowserTest` of the adapter. Do not write an own browser lifecycle. The base class gives each test a new context and page, and `Expect(...)`.
12. Pin `Microsoft.Playwright*` to 1.65.1 in `Directory.Packages.props`. No `Version="*"` or `1.*`.
13. Install the browsers with `playwright.ps1` in the build output, after `dotnet build`. Use the target framework of the project in the path (`net10.0`, `net11.0`), not the `net8.0` of the docs. Microsoft deprecated the dotnet tool `Microsoft.Playwright.CLI`.
14. On Microsoft.Testing.Platform (`MSTest.Sdk`, xUnit v3), Playwright ignores the `<Playwright>` section of `.runsettings`, and nothing reports it. `dotnet test -- Playwright.BrowserName=...` is the VSTest form. Set `BROWSER`, `HEADED`, and `PWDEBUG` as environment variables, and set the base URL, the viewport, and the locale in `ContextOptions()`.
15. Start the app under test with `WebApplicationFactory<Program>`, `UseKestrel(0)`, and `StartServer()` (PW-03). A browser cannot reach the in-memory `TestServer`. Do not override `CreateHost` to build a second host. On .NET 10 and later, remove `public partial class Program { }`: a source generator makes it.

Read `references/dotnet.md` for the project files, the base class, the app server, and CI.

### Agent browser

16. Pin the version of `@playwright/mcp` and `@playwright/cli` in each shared file. This includes a permission rule in `.claude/settings.json`, such as `Bash(npx @playwright/cli@0.1.25:*)`. The 0.0.x line changes about each 2 weeks. `@latest` gives each person another tool set.
17. Playwright MCP is not a security boundary. Write this sentence in the setup docs for the team. `--allowed-origins` does not stop redirects. `--secrets` only replaces text in the output. Give the agent a test account on a local or test app, never a real account or a production URL.
18. Do not enable `browser_run_code_unsafe` for an agent on a page that you do not control. It runs any JavaScript in the server process.
19. A check that must run again goes into a Playwright test (rule 1 to 7). Code from `--codegen` is a draft only.

Read `references/agent-browser.md` for the setup, the options, and the choice between MCP and the CLI.

## Never write

| Wrong | Correct |
|-------|---------|
| `<PackageReference Include="Microsoft.Playwright.MSTest" />` with MSTest 4 | `Microsoft.Playwright.MSTest.v4` |
| A manual `Playwright.CreateAsync()` lifecycle "because MSTest 4 support is not known" | `PageTest` from the `.v4` package |
| `dotnet tool install --global Microsoft.Playwright.CLI` | `pwsh bin/Debug/net10.0/playwright.ps1 install --with-deps chromium` |
| `<Playwright><BrowserName>firefox</BrowserName></Playwright>` in `.runsettings` on MTP | `BROWSER=firefox` as an environment variable |
| `protected override IHost CreateHost(...)` with a second Kestrel host | `factory.UseKestrel(0); factory.StartServer();` |
| `page.locator('table#orders tbody tr')`, `td.status` | `page.getByRole('row').filter({ hasText: orderNumber })` |
| `await page.waitForTimeout(3000);` | `await expect(row).toBeVisible();` |
| `if (await banner.isVisible()) { ... }` as a check | `await expect(banner).toBeVisible();` |
| `webServer: { command, url, port }` | `webServer: { command, url, reuseExistingServer: !process.env.CI }` |
| `readonly submit = this.page.getByRole('button')` in a page object | `this.submit = page.getByRole('button', { name: 'Save' })` in the constructor |
| `page.locator('button:visible')` | `page.getByRole('button').visible()` |
| `npx @playwright/mcp@latest` in `.mcp.json` | `npx @playwright/mcp@0.0.86` |
| `Bash(npx @playwright/mcp@latest:*)` in `.claude/settings.json` | The pinned version of `.mcp.json` in the permission rule too |
| "`--allowed-origins` keeps the agent on our site" | A test account, a test app, and the permissions of the MCP client |
| A healer `test.fixme()` or `test.skip()` merged without review | Classify the red test first (PW-04) |

## Check

1. Run the tests:
   - TypeScript: `npx playwright test`. Pass: each test passed, 0 flaky.
   - .NET: `dotnet test`. Pass: `Test run summary: Passed!`.

   When you added or changed a test, run it 3 times (`npx playwright test --repeat-each=3`, or the .NET run 3 times). Pass: 3 times green. A test that fails one time in 3 is flaky. Fix the wait or the shared data, then run it 3 times again.
2. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.
3. Classify each red test before you change it. The classes are: a defect in the app, a defect in the test, or a flaky test. Done when the report names the class and the proof (trace, screenshot, or log line). A real regression stays red.

## Review checklist

The full list is in `references/review-checklist.md`.

1. The MSTest 4 adapter is `.v4`, with a pinned version. tool (PW-MSTEST-ADAPTER)
2. No deprecated `Microsoft.Playwright.CLI` tool, no `<Playwright>` section on MTP. tool (PW-DOTNET-CLI-TOOL, PW-RUNSETTINGS)
3. Role, label, or text locators. No CSS chains. judge
4. Web-first assertions, no fixed waits. tool (PW-FIXED-WAIT), judge
5. Each test makes its own data and asserts the outcome. judge
6. Sign-in through a setup project and `storageState`, with a test account. judge
7. The .NET app under test runs through `UseKestrel(0)` and `StartServer()`. judge
8. Each MCP and CLI setup has a pinned version. Nobody treats `--allowed-origins` or `--secrets` as security. tool (PW-AGENT-UNPINNED), judge
9. No skip or `fixme` from a test agent without a reason. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Checks": the test run 3 times, and the class of each red test (PW-04).
- Under "Packages": the Playwright version, the adapter, and the MCP or CLI version before and after.
- Under "Decisions": each PW decision that the change uses. Mark each one accepted or assumed.

## References

- `references/typescript.md`: read before you write or review Playwright Test code in TypeScript.
- `references/dotnet.md`: read before you write or review a .NET Playwright test or its project.
- `references/agent-browser.md`: read before you set up or use Playwright MCP or `playwright-cli`.
- `references/decisions.md`: read when new code needs a decision with an ID from the defaults table.
- `references/review-checklist.md`: read before a full review.
- `references/sources.md`: the primary sources, the date of each check, and the facts that are not verified.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
