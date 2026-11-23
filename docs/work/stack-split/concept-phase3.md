# Concept: stack split phase 3

Date: 2026-11-20. Status: approved on 2026-11-21. The owner accepted each recommendation.

Phase 3 adds 2 skills to test-stack and gives `dotnet-testing` a part for xUnit v3. The research is in `research-playwright.md` and `research-reqnroll-xunit-mstest.md` in this folder. Each research file has the versions, the useful options, and the mistakes of AI tools since 2026-05. A probe on Haiku 5.5 and Sonnet 5.5 adds to them.

## Contents

- Version facts that decide the rules
- What the probe showed
- Skills
- Changes to existing skills
- Behaviour test
- Decision sheet
- Releases

## Version facts that decide the rules

- Playwright 1.65.1 (npm 2026-11-13, NuGet 2026-11-18). Node.js 20 is no longer tested. Use Node.js 22 or later.
- MSTest 4 needs the adapter `Microsoft.Playwright.MSTest.v4`. The plain `Microsoft.Playwright.MSTest` does not compile with MSTest 4. The Playwright docs do not name the `.v4` package.
- On Microsoft.Testing.Platform (MTP), Playwright ignores the `<Playwright>` section of `.runsettings`, with no warning. Use environment variables or `testconfig.json`.
- .NET 10 has `WebApplicationFactory.UseKestrel(0)` and `StartServer()`, so a real browser can reach the app. `public partial class Program` is not necessary on .NET 10.
- Microsoft deprecated the `Microsoft.Playwright.CLI` dotnet tool. Install the browsers with `playwright.ps1` in the build output.
- `@playwright/mcp` 0.0.86 and the new `@playwright/cli` 0.1.25. The MCP README says that MCP is not a security boundary. It recommends the CLI with its skill for coding agents.
- Reqnroll 3.3.4 is the latest. MSTest 4 needs Reqnroll 3.2 or later. xUnit v3 uses `Reqnroll.xunit.v3`.
- `xunit.v3` 4.0.3. Version 4 runs on MTP v2 by default. `CollectionBehavior(DisableTestParallelization = true)` does not compile on 4.0 (CS0619).
- MSTest 4.5.3 (MTP 2.5.3) has bug fixes only. MSTest 5 will remove `StringAssert`, `CollectionAssert`, and `[DataTestMethod]`.

## What the probe showed

- TypeScript Playwright: both models write current code. One config error (`webServer` with `url` and `port`).
- Playwright .NET: the old MSTest adapter, the deprecated tool, CSS locators, an own Kestrel host, `Version="*"`.
- Playwright MCP: Haiku calls `--allowed-origins` a security boundary. No model names `playwright-cli`.
- Reqnroll: Haiku picks `Reqnroll.MSTest` 2.4.0 with MSTest 4. Both use `ScenarioContext` string keys.
- xUnit: both models use `xunit.v3` 3.2.0 and the obsolete `CollectionBehavior` form.
- MSTest: no `MSTest.Sdk` and no `global.json`. Haiku writes `[DataRow(100.00)]` for a `decimal` parameter, which fails at run time.

The largest gaps are in .NET setup and versions, not in TypeScript.

## Skills

| Skill | Scope |
|-------|-------|
| `playwright` (new) | One `SKILL.md` that routes to 3 guides: `references/typescript.md` (Playwright Test), `references/dotnet.md` (Microsoft.Playwright with MSTest 4 or xUnit v3), `references/agent-browser.md` (Playwright MCP and `playwright-cli`). Locators, web-first assertions, auth with a setup project, test data, the app under test, traces, CI. |
| `reqnroll` (new) | Reqnroll 3 with MSTest 4 or xUnit v3, and the Gherkin rules (`references/gherkin.md`). Context injection, hooks, Playwright in the bindings, tables, parallel runs, the HTML formatter, the move from SpecFlow. |
| `dotnet-testing` (changed) | Routes on the framework of the project: MSTest 4 (existing text) or xUnit v3 (new `references/xunit-v3.md`). The common rules stay common: test design, names, assertions, fakes, `WebApplicationFactory`, Testcontainers. |

Top traps:

- `playwright`:
  - `.MSTest` adapter with MSTest 4.
  - `.runsettings` on MTP.
  - Own Kestrel host.
  - The deprecated tool.
  - CSS locators.
  - `waitForTimeout`.
  - `url` and `port`.
  - `--allowed-origins` as security.
  - Unpinned MCP version.
  - Healer skips.
- `reqnroll`:
  - Reqnroll 2.x with MSTest 4.
  - `ScenarioContext` keys.
  - A shared browser context.
  - A password in a feature file.
  - Imperative UI steps.
  - More than one `When`.
  - Tags on `Examples`.
  - A throw in `[AfterScenario]`.
- `dotnet-testing`:
  - XUnit: `xunit.v3` 3.x, `CollectionBehavior`, VSTest packages, `Task` in `IAsyncLifetime`, no `TestContext.Current.CancellationToken`.
  - MSTest: decimal `DataRow`, no `Parallelize`, no `MSTest.Sdk` pin, new `StringAssert` code.

Each new skill follows the stack skill template: `SKILL.md` under 300 lines, `references/decisions.md`, a review checklist, sources, and a `rules.json` with 3 to 6 rules.

## Changes to existing skills

- `dotnet-testing`: the description drops "Not for xUnit" and names xUnit v3. Playwright end-to-end tests go to `test-stack:playwright`. The MSTest deltas go into `references/mstest-4.md`: version 4.5.3, MSTEST0084 to 0089, the decimal `DataRow`, discovery fails on a type-load error, and the MSTest 5 removals.
- `migrate-fluentassertions`: no change.
- `dev-team:stack-index`: 2 new rows and the xUnit route.

## Behaviour test

The same workflow as phase 2: 8 scenarios, 6 agents (2 Haiku, 2 Sonnet, 2 Opus), 3 arms, 144 runs. Each run gets script checks and one blind Sonnet judge.

| No | Scenario | Skill | Arm "current" (dev-stack 1.3.0) |
|----|----------|-------|---------------------------------|
| S1 | A TypeScript Playwright suite: login once in a setup project, then check the order list. With `playwright.config.ts` | `playwright` | `angular` |
| S2 | A Playwright .NET test with MSTest 4 for an ASP.NET Core 10 app in the same repo. With the test `.csproj` | `playwright` | `dotnet-testing` |
| S3 | Set up Playwright MCP for the team in `.mcp.json`, and a note when to use it | `playwright` | `secure-coding` |
| S4 | A Reqnroll feature and step definitions for "cancel an order", MSTest 4, Playwright in the steps | `reqnroll` | `dotnet-testing` |
| S5 | Rewrite an imperative feature file (seed) into declarative scenarios | `reqnroll` | `dotnet-testing` |
| S6 | A new xUnit v3 test project with a PostgreSQL container for the whole assembly, no parallel database tests | `dotnet-testing` | `dotnet-testing` |
| S7 | Move a small xUnit v2 test project (seed) to xUnit v3 | `dotnet-testing` | `dotnet-testing` |
| S8 | MSTest 4 data-driven tests for a price calculation with `decimal` values, with the test project | `dotnet-testing` | `dotnet-testing` |

- Success (brief): the new arm passes more checks than the baseline on each model, and writes no old pattern.
- S6, S7, and S8 compare the new `dotnet-testing` with its 1.3.0 text. This shows the effect of the xUnit part and the MSTest deltas.
- Cost estimate: about $70, as in phase 2. Cap: $150. At most 4 agents at the same time.

Before the build, one agent compiles the facts that are not verified, one build at a time:

- `PageTest` from `.MSTest.v4` on `MSTest.Sdk` with MTP.
- The `.Xunit.v3` adapter with `xunit.v3` 4.x.
- `UseKestrel(0)` and `StartServer()`, and how the test reads the address.
- Reqnroll 3.3.4 with MSTest 4 and Playwright in `[BeforeScenario]`.
- The same on `net11.0`.

## Decision sheet

The recommendation is the answer unless the owner changes the row.

| ID | Question | Recommendation |
|----|----------|----------------|
| P3-01 | Playwright MCP or `playwright-cli` for a browser check by a coding agent? | Both are in the skill. A project that has MCP set up keeps it. For a new setup, the CLI is the default for a short check, as the README advises. MCP is for long exploration. |
| P3-02 | Which .NET adapter? | `Microsoft.Playwright.MSTest.v4` with `PageTest` in MSTest 4 projects. `Microsoft.Playwright.Xunit.v3` in xUnit v3 projects. |
| P3-03 | How does a .NET test start the app under test? | `WebApplicationFactory` with `UseKestrel(0)` and `StartServer()` for an app in the same repo. `Aspire.Hosting.Testing` when the repo has an AppHost (`dev-stack:aspire`). A deployed URL only for a smoke test. |
| P3-04 | Playwright test agents (planner, generator, healer)? | Not by default. The skill says: classify a red test first, and never accept a skip from the healer without review. |
| P3-05 | Reqnroll and Playwright: `PageTest` base class or own hooks? | Own hooks: a new browser context and page for each scenario in `[BeforeScenario]`, through context injection. The compile check confirms the form. |
| P3-06 | Default framework for a new .NET test project? | MSTest 4 stays the default (TEST-01). xUnit v3 only where the repo uses xUnit. The skill does not move a repo from one framework to the other unless the task asks. |
| P3-07 | Language of feature files? | The language of the existing features. English in a new repo. Dutch with `# language: nl` when the product owner writes Dutch. |
| P3-08 | Compile check on .NET 10 and .NET 11 before the build? | Yes, in the scratch folder, one build at a time. |
| P3-09 | Does `frontend-stack:angular` point to `playwright` for the browser check? | Yes, one pointer line in its description. frontend-stack 1.1.1. |

## Releases

- test-stack 1.1.0: `playwright`, `reqnroll`, and the xUnit part and MSTest deltas in `dotnet-testing`.
- dev-team 1.1.2: the new rows in `stack-index`.
- frontend-stack 1.1.1: the pointer in `angular` (P3-09).
