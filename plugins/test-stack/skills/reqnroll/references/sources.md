# Sources

Checked on 2026-11-21. The facts of this skill come from these primary sources and from a compile check. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Compile check
- Documentation and releases
- Issues
- Facts that are not verified

## Versions

| Item | Version on 2026-11-21 | Source |
|------|-----------------------|--------|
| `Reqnroll`, `Reqnroll.MsTest`, `Reqnroll.xunit.v3`, `Reqnroll.NUnit`, `Reqnroll.TUnit` | 3.3.4 (2026-03-23) | [NuGet Reqnroll](https://www.nuget.org/packages/Reqnroll) |
| `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 (2026-03-23). Needs Reqnroll `[3.3.4, 4.0.0)` | [NuGet Reqnroll DI plugin](https://www.nuget.org/packages/Reqnroll.Microsoft.Extensions.DependencyInjection) |
| `Reqnroll.SpecFlowCompatibility` | 3.3.3 (2026-01-27) | [NuGet Reqnroll.SpecFlowCompatibility](https://www.nuget.org/packages/Reqnroll.SpecFlowCompatibility) |
| MSTest, `MSTest.Sdk` | 4.5.3 (2026-11-11) | [NuGet MSTest.Sdk](https://www.nuget.org/packages/MSTest.Sdk) |
| `xunit.v3` | 4.0.3 (2026-11-09) | [NuGet xunit.v3](https://www.nuget.org/packages/xunit.v3) |
| `Microsoft.Playwright` | 1.65.1 (2026-11-18) | [NuGet Microsoft.Playwright](https://www.nuget.org/packages/Microsoft.Playwright) |

## Compile check

On 2026-11-21, one agent built and ran these projects, one build at a time, on SDK 10.0.404 (`net10.0`) and SDK 11.0.100 (`net11.0`):

- `Reqnroll.MsTest` 3.3.4 on `MSTest.Sdk` 4.5.3 with `Microsoft.Playwright` 1.65.1 and `WebApplicationFactory` with `UseKestrel(0)`: 0 warnings. Context injection of 2 POCO classes, the browser in `[BeforeTestRun]`, a context and page for each scenario, and a trace only for the failed scenario.
- `Reqnroll.xunit.v3` 3.3.4 with `xunit.v3` 4.0.3: 0 warnings, with an injected `Xunit.ITestOutputHelper`.
- `{decimal}` in a Cucumber expression, with MSTest and with xUnit v3.
- `global.json` without the `test` section: the VSTest error on SDK 10 and on SDK 11.
- `dotnet test --disable-build-servers` in MTP mode: "Unknown option", exit code 5.

## Documentation and releases

- [Reqnroll releases](https://github.com/reqnroll/Reqnroll/releases) and [CHANGELOG.md](https://github.com/reqnroll/Reqnroll/blob/main/CHANGELOG.md): MSTest 4 in 3.2.0 and xUnit v3 in 3.1.0. `Expression` in 3.0.0, `NotImplementedException` in 3.3.0. `[AfterScenario]` for skipped scenarios and MSTest tags in 3.2.1.
- [Compatibility](https://docs.reqnroll.net/latest/installation/compatibility.html): the supported .NET versions.
- [MSTest integration](https://docs.reqnroll.net/latest/integrations/mstest.html): `TestContext` injection, tags on `Examples`, `allowRowTests`.
- [xUnit integration](https://docs.reqnroll.net/latest/integrations/xunit.html): `ITestOutputHelper` (the docs show the v2 namespace).
- [Scenario context](https://docs.reqnroll.net/latest/automation/scenario-context.html) and [parallel execution](https://docs.reqnroll.net/latest/execution/parallel-execution.html): `ScenarioContext.Current`, parallel scopes, `FeatureContext` with `MethodLevel`.
- [Hooks](https://docs.reqnroll.net/latest/automation/hooks.html) and [scoped bindings](https://docs.reqnroll.net/latest/automation/scoped-bindings.html): static hooks, `Order`, `[Scope]`.
- [DataTable helpers](https://docs.reqnroll.net/latest/automation/datatable-helpers.html) and [step argument conversions](https://docs.reqnroll.net/latest/automation/step-argument-conversions.html).
- [Formatters](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html): `formatters/html/outputFilePath` and `REQNROLL_FORMATTERS_HTML`.
- [Migrating from SpecFlow](https://docs.reqnroll.net/latest/guides/migrating-from-specflow.html) and [SpecFlow end of life](https://reqnroll.net/news/2025/01/specflow-end-of-life-has-been-announced/).
- [Playwright trace viewer for .NET](https://github.com/microsoft/playwright/blob/main/docs/src/trace-viewer-intro-csharp.md): trace start and stop with a path.
- [Gherkin reference](https://cucumber.io/docs/gherkin/reference/), [better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/), [anti-patterns](https://cucumber.io/docs/guides/anti-patterns/), and [gherkin-languages.json](https://github.com/cucumber/gherkin/blob/main/gherkin-languages.json): the Gherkin rules and the Dutch keywords.

## Issues

- [Reqnroll #1117](https://github.com/reqnroll/Reqnroll/issues/1117): a throw in `[AfterScenario]` skips the later hooks.
- [Reqnroll #1106](https://github.com/reqnroll/Reqnroll/issues/1106): CS0246 on `[Binding]` when the project name contains the name of a test framework.
- [Reqnroll discussion #1125](https://github.com/reqnroll/Reqnroll/discussions/1125) and #1126: no tests found on .NET Framework 4.8.1 with MSTest 4.5.1.
- [Reqnroll #1083](https://github.com/reqnroll/Reqnroll/issues/1083): a scenario missing from the HTML report after a cleanup error.
- [Reqnroll #822](https://github.com/reqnroll/Reqnroll/issues/822): `Examples` under a plain `Scenario`.

## Facts that are not verified

The skill does not use these as rules. Check them before a rule uses them.

- Reqnroll with Playwright and `MethodLevel` parallel runs. The compile check ran without a parallel setting.
- Reqnroll with `xunit.v3` 4.0 parallel modes (`ParallelMode.All`) and `addNonParallelizableMarkerForTags`.
- The date of a Reqnroll 4.0 release.
- The study that LLMs write too few failure-path scenarios (arXiv 2607.01980). The research read only the abstract. Rule 5 also follows from the Cucumber guidance.
- Official support of .NET 11 by Reqnroll. The compatibility page stops at .NET 10. The compile check passed on `net11.0`.
