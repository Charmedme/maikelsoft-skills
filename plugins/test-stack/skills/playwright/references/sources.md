# Sources

Checked on 2026-11-20. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months. Playwright has a minor release about each month, and the MCP server about each 2 weeks.

## Contents

- Versions
- Documentation and releases
- Issues
- Facts that are not verified

## Versions

| Item | Version on 2026-11-20 | Source |
|------|-----------------------|--------|
| `@playwright/test`, `playwright` | 1.65.1 (2026-11-13) | [npm @playwright/test](https://www.npmjs.com/package/@playwright/test) |
| `Microsoft.Playwright` and the adapters `.MSTest`, `.MSTest.v4`, `.Xunit`, `.Xunit.v3`, `.NUnit` | 1.65.1 (2026-11-18). Target `netstandard2.0` | [NuGet Microsoft.Playwright](https://www.nuget.org/packages/Microsoft.Playwright), [NuGet Microsoft.Playwright.MSTest.v4](https://www.nuget.org/packages/Microsoft.Playwright.MSTest.v4) |
| `Microsoft.Playwright.CLI` (dotnet tool) | 1.2.3 (2022-03-16). Deprecated | [NuGet Microsoft.Playwright.CLI](https://www.nuget.org/packages/Microsoft.Playwright.CLI) |
| `@playwright/mcp` | 0.0.86 (2026-11-18) | [npm @playwright/mcp](https://www.npmjs.com/package/@playwright/mcp) |
| `@playwright/cli` | 0.1.25 (2026-11-18) | [npm @playwright/cli](https://www.npmjs.com/package/@playwright/cli) |
| `@playwright/experimental-ct-react` | 1.62.1 (2026-07-30). No updates since 1.63 | [npm @playwright/experimental-ct-react](https://www.npmjs.com/package/@playwright/experimental-ct-react) |
| MSTest, `MSTest.Sdk` | 4.5.3 (2026-11-11) | [NuGet MSTest.Sdk](https://www.nuget.org/packages/MSTest.Sdk) |
| `xunit.v3` | 4.0.3 (2026-11-09) | [NuGet xunit.v3](https://www.nuget.org/packages/xunit.v3) |
| Browsers in 1.65.1 | Chromium 159, Firefox 159, WebKit 27.2 | [Playwright v1.65.0 release notes](https://github.com/microsoft/playwright/releases/tag/v1.65.0) |

## Documentation and releases

- [Playwright releases](https://github.com/microsoft/playwright/releases): 1.59 to 1.65, the source of the new APIs and the breaking changes in `SKILL.md`.
- [Playwright for .NET releases](https://github.com/microsoft/playwright-dotnet/releases): `ActionTimeout`, `NavigationTimeout`, `PlaywrightAssertionException`, and `libatomic1` in 1.65.
- [Authentication](https://github.com/microsoft/playwright/blob/main/docs/src/auth.md): the setup project and `storageState`.
- [webServer plugin source](https://github.com/microsoft/playwright/blob/main/packages/playwright/src/plugins/webServerPlugin.ts): the error for `url` and `port` together.
- [Clock](https://github.com/microsoft/playwright/blob/main/docs/src/clock.md): `install`, `setFixedTime`, `pauseAt`, `runFor`.
- [Component tests](https://github.com/microsoft/playwright/blob/main/docs/src/test-components-js.md): stories, galleries, and the migration guide.
- [Test agents](https://github.com/microsoft/playwright/blob/main/docs/src/test-agents-js.md): `init-agents`, the planner, the generator, and the healer.
- [.NET intro](https://github.com/microsoft/playwright/blob/main/docs/src/intro-csharp.md) and [.NET test runners](https://github.com/microsoft/playwright/blob/main/docs/src/test-runners-csharp.md): base classes, `playwright.ps1`, parallel runs, retries.
- [Playwright.MSTest.v4 project file](https://github.com/microsoft/playwright-dotnet/blob/main/src/Playwright.MSTest.v4/Playwright.MSTest.v4.csproj): the namespace `Microsoft.Playwright.MSTest`.
- [WebApplicationFactory.UseKestrel](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.usekestrel?view=aspnetcore-10.0) and [StartServer](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1.startserver?view=aspnetcore-11.0).
- [What is new in ASP.NET Core 10](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0#miscellaneous): the generated `public partial class Program`.
- [Playwright MCP README](https://github.com/microsoft/playwright-mcp/blob/main/README.md): options, tools, "not a security boundary", and the advice to use the CLI for coding agents.
- [Playwright MCP releases](https://github.com/microsoft/playwright-mcp/releases): `--redact-url-params` and `isolatedContext` in 0.0.84.
- [playwright-cli README](https://github.com/microsoft/playwright-cli): install and skills.
- [Playwright Workspaces](https://learn.microsoft.com/rest/api/playwright/): the end of Microsoft Playwright Testing on 2026-03-08.

## Issues

- [playwright-dotnet#3265](https://github.com/microsoft/playwright-dotnet/issues/3265): the compile error of `Microsoft.Playwright.MSTest` with MSTest 4, and the `.v4` fix.
- [playwright-dotnet#3124](https://github.com/microsoft/playwright-dotnet/issues/3124) and [#3375](https://github.com/microsoft/playwright-dotnet/issues/3375): Playwright ignores `.runsettings` on Microsoft.Testing.Platform. `testconfig.json` `environmentVariables` on MTP 2.3 or later.
- [playwright-dotnet#3377](https://github.com/microsoft/playwright-dotnet/pull/3377): the open pull request for `Microsoft.Playwright.Testing.Platform`.
- [playwright-dotnet#3287](https://github.com/microsoft/playwright-dotnet/issues/3287): Visual Studio Test Explorer ignores `LaunchOptions`.
- [playwright#43675](https://github.com/microsoft/playwright/issues/43675): a direct fix by an agent is often better than the healer.
- [playwright#41954](https://github.com/microsoft/playwright/issues/41954): the healer asks the user questions against its definition.
- [playwright#42743](https://github.com/microsoft/playwright/issues/42743): invalid TypeScript in the generator definition of 1.63.0.
- [playwright-mcp#1758](https://github.com/microsoft/playwright-mcp/issues/1758): MCP is not a security boundary.
- [playwright-mcp#1783](https://github.com/microsoft/playwright-mcp/issues/1783): agents navigate to guessed domains.
- [playwright-mcp#1861](https://github.com/microsoft/playwright-mcp/issues/1861): wrong environment variables in the README, `PLAYWRIGHT_MCP_SANDBOX=false` is correct.
- [playwright-mcp#1836](https://github.com/microsoft/playwright-mcp/issues/1836): relative paths in a `--config` file.

## Facts that are not verified

The skill does not use these as rules. Check them before a rule uses them.

- That the `netstandard2.0` .NET packages run on .NET 11.
- That `Microsoft.Playwright.Xunit.v3` 1.65.1 works with `xunit.v3` 4.0.3, and `Microsoft.Playwright.MSTest.v4` with `MSTest.Sdk` 4.5.3 on MTP without extra properties.
- How a test reads the Kestrel address after `StartServer()`.
- The Aspire call that reads the endpoint for a Playwright test.
- `Microsoft.Playwright.Program.Main(["install"])` as an install path. The current docs do not name it.
- `page.perform` and `--run-agents`. The CLI docs name them, the API docs do not.
- Whether a released `create-playwright` writes `.claude/skills/playwright/SKILL.md`.
- The blog reports about AI mistakes with fixed waits and `.first()`. They name no model and give no run data.
