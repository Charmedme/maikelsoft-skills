# Research: Reqnroll, Gherkin, xUnit v3, and MSTest 4

Date: 2026-11-20. Status: research for phase 3 of the stack split. Input for the new skill `test-stack:reqnroll` and for the xUnit part and the MSTest part of `test-stack:dotnet-testing`.

Marks:

- **verified, source**: the linked page, release note, issue, package metadata, or source file says it.
- **verified, probe**: the model wrote it in the probe of this research, or the build check of this research showed it (see "Probe method").
- **not verified**: from memory or from one weak source. Check it before a rule uses it.

## Contents

- Summary of versions
- Probe method
- Reqnroll
- Gherkin rules
- xUnit v3
- MSTest 4: deltas since 2026-10-27
- Recent AI mistakes
- New traps for the skills
- Facts that are not verified

## Summary of versions

All dates are the publish dates on nuget.org (verified, source: the NuGet registration API for each package).

| Package | Latest stable | Date | Note |
|---------|---------------|------|------|
| Reqnroll, Reqnroll.MsTest, Reqnroll.xunit.v3, Reqnroll.NUnit, Reqnroll.TUnit | 3.3.4 | 2026-03-23 | No release after this date. `main` has a "vNext" section |
| Reqnroll.Microsoft.Extensions.DependencyInjection | 3.3.4 | 2026-03-23 | Needs Reqnroll `[3.3.4, 4.0.0)` |
| Reqnroll.SpecFlowCompatibility | 3.3.3 | 2026-01-27 | One patch behind the core |
| Reqnroll IDE Support (VS, VS Code, Rider) | v2026.2 | 2026-10-25 | Preview |
| xunit.v3 (depends on xunit.v3.mtp-v2) | 4.0.3 | 2026-11-09 | 4.0.0 on 2026-08-14 |
| xunit.v3.mtp-off (VSTest only) | 4.0.3 | 2026-11-09 | |
| xunit.v3.mtp-v1 | 3.2.2 | 2026-01-14 | No 4.x. MTP v1 support ended in 4.0 |
| xunit.runner.visualstudio | 4.0.1 | 2026-10-09 | Only for VSTest |
| xunit (v2) | 2.9.3 | 2025-01-08 | No new v2 release |
| MSTest, MSTest.Sdk | 4.5.3 | 2026-11-11 | The skill names 4.5.2 |
| Microsoft.Testing.Platform | 2.5.3 | 2026-11-11 | The skill names 2.5.2 |
| Microsoft.NET.Test.Sdk | 18.12.0 | 2026-11-11 | |
| Microsoft.Playwright, .MSTest.v4, .Xunit.v3 | 1.65.1 | 2026-11-18 | |
| Testcontainers, Testcontainers.XunitV3 | 4.17.0 | 2026-11-16 | The skill names 4.16.0 |
| AwesomeAssertions | 9.6.0 | 2026-08-20 | No change |
| FluentAssertions | 8.11.0 (7.2.2 is the last 7.x) | 2026-09-14 | Keep `[7.2.2,8.0.0)` |

## Probe method

- Tool: `claude -p` (headless), one call at a time, from an empty scratch folder, with stdin closed.
- The research removed the variables of the parent session (`CLAUDECODE`, each `CLAUDE_*` variable).
- Flags: `--tools ""`, `--disallowedTools` (Skill, Agent, Bash, Read, Edit, Write, WebFetch, WebSearch, Glob, Grep), `--strict-mcp-config`, `--disable-slash-commands`, `--setting-sources ""`, `--no-session-persistence`.
- Models: `haiku` (Haiku 5.5) and `sonnet` (Sonnet 5.5). 5 prompts for each model, 10 calls, total cost $0.19. Each prompt starts with "Today is 2026-11-20. You have no tools. Use the latest stable versions." and asks for code only.
- Prompts: rq1 Reqnroll feature and steps for an order cancel with MSTest. rq2 Reqnroll with Playwright and MSTest, one page per scenario, a trace on failure. xu1 xUnit v3 class with a shared PostgreSQL fixture. xu2 xUnit v3 data-driven tests with output, cancellation, and no parallel runs. ms1 MSTest 4 data-driven tests with decimal amounts and an exception test.
- Build check: the research built and ran 3 small projects on SDK 10.0.404. The results have the mark "verified, probe" with the word "build".

## Reqnroll

### Versions and support

- Reqnroll 3.3.4 is the latest release (2026-03-23). The last commit on `main` is from 2026-11-05. The changelog section "vNext" has only dependency updates and one bug fix. (verified, source, [releases](https://github.com/reqnroll/Reqnroll/releases), [CHANGELOG.md](https://github.com/reqnroll/Reqnroll/blob/main/CHANGELOG.md))
- The compatibility page lists .NET Framework 4.6.2, 4.7.2, 4.8.1, .NET 8, 9, and 10. It does not list .NET 11. (verified, source, [compatibility](https://docs.reqnroll.net/latest/installation/compatibility.html))
- MSTest 4 support starts in Reqnroll 3.2.0 (2025-10-21). Reqnroll 2.x does not support MSTest 4. (verified, source, CHANGELOG 3.2.0)
- xUnit v3 support starts in 3.1.0. The package is `Reqnroll.xunit.v3`. `Reqnroll.xUnit` is for xUnit v2. (verified, source, CHANGELOG 3.1.0)
- `Reqnroll.MsTest` 3.3.4 needs `MSTest.TestFramework >= 2.2.8`. `Reqnroll.xunit.v3` needs `xunit.v3.extensibility.core >= 2.0.0`. `Reqnroll.NUnit` needs `NUnit >= 3.13.1`. (verified, source, NuGet metadata)
- Reqnroll 3.3.4 with `MSTest.Sdk` 4.5.3 on Microsoft.Testing.Platform (MTP) and `net10.0`: build and run pass. The test used a `Rule`, a `Scenario Outline`, an injected POCO, an injected `TestContext`, and `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`. (verified, probe, build)
- Reqnroll 3.3.4 with `xunit.v3` 4.0.3 (MTP v2) on `net10.0`: build and run pass, with an injected `ITestOutputHelper` and `TestContext.Current.CancellationToken`. No Reqnroll release names xUnit 4.0. (verified, probe, build)
- The Reqnroll MSTest docs still list `MSTest.TestAdapter` and `Microsoft.NET.Test.Sdk`. They do not name `MSTest.Sdk` or MTP. (verified, source, [MSTest integration](https://docs.reqnroll.net/latest/integrations/mstest.html))
- Open issue: Reqnroll 3.3.x does not discover tests in a .NET Framework 4.8.1 project with MSTest 4.5.1 (#1126, 2026-10-23). Keep the versions of `MSTest.TestFramework` and `MSTest.TestAdapter` the same. (verified, source, [discussion #1125](https://github.com/reqnroll/Reqnroll/discussions/1125))
- A test project name or namespace that contains the name of a test framework can make the compiler drop `Reqnroll.dll` (CS0246 on `[Binding]`). Set `<RootNamespace>`. (verified, source, [#1106](https://github.com/reqnroll/Reqnroll/issues/1106))

### Move from SpecFlow

Source: [migrating from SpecFlow](https://docs.reqnroll.net/latest/guides/migrating-from-specflow.html) (verified, source).

- SpecFlow reached end of life on 2024-12-31. Use Reqnroll for new work. (verified, source, [Reqnroll news](https://reqnroll.net/news/2025/01/specflow-end-of-life-has-been-announced/))
- Packages: `SpecFlow.*` becomes `Reqnroll.*`. Remove `CucumberExpressions.SpecFlow.*`. Reqnroll has Cucumber Expressions built in.
- Namespaces: `TechTalk.SpecFlow` becomes `Reqnroll`. `BoDi` becomes `Reqnroll.BoDi`. `ISpecFlowOutputHelper` becomes `IReqnrollOutputHelper`. The Assist helpers are in the `Reqnroll` namespace.
- `Reqnroll.SpecFlowCompatibility` keeps `TechTalk.SpecFlow` names for a fast first step.
- Configuration: `reqnroll.json`. Reqnroll also reads `specflow.json`. Use the schema `https://schemas.reqnroll.net/reqnroll-config-latest.json`.
- SpecFlow+ LivingDoc has no port. The Reqnroll HTML formatter is "the default replacement" since 3.0. (verified, source, [formatters](https://docs.reqnroll.net/latest/reporting/reqnroll-formatters.html))
- Reqnroll generates async test code. SpecFlow generated sync code.

### Bindings, context, and DI

- Reqnroll has context injection. A binding class gets each POCO through its constructor. Reqnroll makes one instance for each scenario and shares it between binding classes. (verified, probe, build)
- `ScenarioContext.Current`, `FeatureContext.Current`, and `ScenarioStepContext.Current` are obsolete. They fail in parallel runs. Inject `ScenarioContext` or a POCO. (verified, source, [scenario context](https://docs.reqnroll.net/latest/automation/scenario-context.html), [parallel execution](https://docs.reqnroll.net/latest/execution/parallel-execution.html))
- MSTest: inject `TestContext` in a binding constructor. Static `[BeforeTestRun]` and `[AfterTestRun]` get `TestContext` as a parameter. (verified, source, MSTest integration)
- xUnit: inject `ITestOutputHelper` from the scenario container. The docs sample uses the v2 type `Xunit.Abstractions.ITestOutputHelper`. In v3 the type is `Xunit.ITestOutputHelper`. (verified, source, [xUnit integration](https://docs.reqnroll.net/latest/integrations/xunit.html). (verified, probe, build)
- `Reqnroll.Microsoft.Extensions.DependencyInjection`: a static method with `[ScenarioDependencies]` returns an `IServiceCollection`. `AutoRegisterBindings` is `true` by default. `ScopeLevel` is `Scenario` (default) or `Feature`. (verified, source, [ScenarioDependenciesAttribute.cs](https://github.com/reqnroll/Reqnroll/blob/main/Plugins/Reqnroll.Microsoft.Extensions.DependencyInjection.ReqnrollPlugin/ScenarioDependenciesAttribute.cs))
- Step attributes: `Regex` and `regex` are now `Expression` and `expression`. `ExpressionType` selects Cucumber expression or regular expression. (verified, source, CHANGELOG 3.0.0)

### Hooks, scopes, and transformations

From [hooks](https://docs.reqnroll.net/latest/automation/hooks.html) and [scoped bindings](https://docs.reqnroll.net/latest/automation/scoped-bindings.html) (verified, source):

- `[BeforeTestRun]`, `[AfterTestRun]`, `[BeforeFeature]`, and `[AfterFeature]` must be static. Use parameter injection in them.
- `[BeforeScenario]`, `[AfterScenario]`, `[BeforeScenarioBlock]`, `[BeforeStep]`, and their After forms use the scenario container.
- Hooks of one type run in an unpredictable order. Set `Order` when the order is important.
- `[Scope(Tag = "web")]` limits a hook or a step. More than one criterion in one `[Scope]` is an AND. The docs call feature-coupled steps an anti-pattern.
- `[StepArgumentTransformation]` with an optional expression, `Order`, and `Name` (a named Cucumber parameter type). (verified, source, [step argument conversions](https://docs.reqnroll.net/latest/automation/step-argument-conversions.html))
- Tables: the type is `DataTable`. Helpers: `CreateInstance<T>`, `CreateSet<T>`, `CompareToInstance<T>`, `CompareToSet<T>`. `CreateInstance` also takes a value tuple. (verified, source, [DataTable helpers](https://docs.reqnroll.net/latest/automation/datatable-helpers.html))
- Since 3.2.1, `[AfterScenario]` runs when a scenario ends as inconclusive or skipped. (verified, source, CHANGELOG 3.2.1)
- Open: `[AfterScenario]` does not run when an earlier `[AfterScenario]` throws (#1117, 2026-09-29). Do not throw from a cleanup hook. (verified, source, [#1117](https://github.com/reqnroll/Reqnroll/issues/1117))
- Since 3.3.0, `NotImplementedException` is a failure again, not "pending". (verified, source, CHANGELOG 3.3.0)

### Parallel runs

From [parallel execution](https://docs.reqnroll.net/latest/execution/parallel-execution.html) (verified, source):

- MSTest, NUnit, and TUnit run scenarios in parallel. xUnit runs features (classes) in parallel only.
- MSTest: `[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]` for features, `MethodLevel` for scenarios.
- With method-level parallel runs, `[BeforeFeature]`, `[AfterFeature]`, and `FeatureContext` run once for each feature and each thread, not once for each feature. Use class level, or keep state out of `FeatureContext`.
- The setting `generator/addNonParallelizableMarkerForTags` in `reqnroll.json` marks tagged features as not parallel.
- MSTest does not send tags on `Examples` blocks to the runner. The workaround is `"generator": {"allowRowTests": false}`. (verified, source, MSTest integration)

### Reqnroll with Playwright

Reqnroll has no official Playwright guide. Its page-object guide uses Selenium. (verified, source, [page object model](https://docs.reqnroll.net/latest/guides/page-object-model.html)) The pattern below combines the hook rules and the Playwright docs:

- `IPlaywright` and `IBrowser` once for each test run, in a static `[BeforeTestRun]`. Close them in `[AfterTestRun]`.
- A new `IBrowserContext` and `IPage` for each scenario, in `[BeforeScenario]`. Keep them in an injected POCO, not in `ScenarioContext` string keys.
- Start tracing in `[BeforeScenario]` with `Screenshots`, `Snapshots`, and `Sources`. In `[AfterScenario]`, stop with a `Path` only when `ScenarioContext.TestError` is not null. Close the context in a `finally`. (verified, source, [Playwright trace viewer for .NET](https://github.com/microsoft/playwright/blob/main/docs/src/trace-viewer-intro-csharp.md))
- Use `Expect(...)` web-first assertions and role or label locators.
- `Microsoft.Playwright.MSTest.v4` gives `PageTest` base classes for MSTest 4 (verified, source).
- A Reqnroll binding class cannot use these base classes, because Reqnroll generates the test class (not verified).
- MTP has a retry extension for MSTest, NUnit, and xUnit v3. Its major version must match the MTP major version of the runner. (verified, source, [Playwright test runners for .NET](https://github.com/microsoft/playwright/blob/main/docs/src/test-runners-csharp.md))

### Formatters and tools

- `formatters` in `reqnroll.json`: `html` (one HTML page, the LivingDoc replacement) and `message` (Cucumber Messages `.ndjson`). An environment variable such as `REQNROLL_FORMATTERS_HTML` can turn one on. Since 3.3.0 the output path takes variables for build data and time. (verified, source, formatters, CHANGELOG 3.3.0)
- Open: a scenario is sometimes missing from the HTML report after a cleanup error (#1083). (verified, source, [#1083](https://github.com/reqnroll/Reqnroll/issues/1083))
- `REQNROLL_DRY_RUN=true` runs without binding code, to find undefined steps. (verified, source, CHANGELOG 3.0.0)
- `reqnroll.json` `"language": {"feature": "nl-NL"}` sets the default feature language. (verified, source, #1106)
- IDE: "Reqnroll IDE Support" for Visual Studio, VS Code, and Rider is in preview since 2026-10-21 (v2026.2 on 2026-10-25). (verified, source, [announcement](https://github.com/reqnroll/Reqnroll/discussions/1123), [releases](https://github.com/reqnroll/Reqnroll.IdeSupport/releases))
- `dotnet new` templates support .NET 10 and xUnit v3 since 3.3.4. The template no longer offers FluentAssertions. (verified, source, CHANGELOG 3.3.4)

## Gherkin rules

Sources: [Gherkin reference](https://cucumber.io/docs/gherkin/reference/), [better Gherkin](https://cucumber.io/docs/bdd/better-gherkin/), [anti-patterns](https://cucumber.io/docs/guides/anti-patterns/) (verified, source), and [gherkin-languages.json](https://github.com/cucumber/gherkin/blob/main/gherkin-languages.json).

- **Declarative steps.** A step tells the behaviour and the business result, not clicks and keys. "When the customer cancels the order", not "When I click the Cancel button". UI details go into the step definition or a page object.
- **One behaviour in each scenario.** One `When` (one action), then `Then` steps for its observable result. More `When` steps mean more than one behaviour. Split the scenario.
- **Order.** `Given` (context), then `When` (action), then `Then` (result). No `Given` after a `When`. No action in a `Then`.
- **Observable result.** A `Then` checks what the user or an external system can see. A database row is usually not observable.
- **`Rule`** (since Gherkin 6) groups the scenarios of one business rule. A `Rule` can have its own `Background`. Reqnroll exposes it as `RuleInfo` on `ScenarioContext`. (verified, probe, build: a `Rule` runs in Reqnroll 3.3.4)
- **`Background`** holds only `Given` steps that each scenario of the feature or rule needs. Keep it short. Incidental details go there, essential details stay in the scenario.
- **`Scenario Outline` and `Examples`.** Use them when only data changes. Name the columns in business words. Each row is one test. A `Scenario` (not an outline) with `Examples` is an error. (verified, source, [#822](https://github.com/reqnroll/Reqnroll/issues/822))
- **Language.** `# language: nl` on the first line. Dutch keywords: `Functionaliteit`, `Regel`, `Achtergrond`, `Scenario` or `Voorbeeld`, `Abstract Scenario`, `Voorbeelden`, `Gegeven` or `Stel`, `Als` or `Wanneer`, `Dan`, `En`, `Maar`.
- **Step reuse.** Write steps in domain words, so that features share them. Do not make one step for each screen. A step text with a parameter replaces many near-copies (step explosion). Do not couple a step to one feature with `[Scope(Feature = ...)]`.
- **Conjunction steps.** Do not put two actions in one step ("Given I have shades and a brand new Mustang"). Split them.
- **Tags** for filtering and hooks: `@web`, `@slow`, `@wip`. In MSTest, feature tags become `TestCategory` (fixed in 3.2.1). Filter with `dotnet test --filter "TestCategory=web"`.
- **Secrets.** No passwords or tokens in a feature file. Read them from the environment in the step definition.

## xUnit v3

### Versions and runner

- xUnit v3 4.0.0 shipped on 2026-08-14. 4.0.3 is current (2026-11-09). (verified, source, [4.0.0 notes](https://xunit.net/releases/v3/4.0.0), NuGet)
- `xunit.v3` 4.x depends on `xunit.v3.mtp-v2`. MTP v2 is the default. 4.0 ended support for MTP v1 and for Mono. `xunit.v3.mtp-off` keeps VSTest. (verified, source)
- On the .NET 10 SDK, `dotnet test` with MTP needs `"test": {"runner": "Microsoft.Testing.Platform"}` in `global.json`. The 4.0 notes point to this for the VSTest error. One `global.json` serves each test project, so MSTest and xUnit projects in one repository must use the same mode. (verified, source)
- Each xUnit v3 test project is an executable: `<OutputType>Exe</OutputType>`. Targets: `net8.0` or later, or `net472`. (verified, source, [migration](https://xunit.net/docs/getting-started/v3/migration))
- A project on MTP does not need `xunit.runner.visualstudio` or `Microsoft.NET.Test.Sdk`, which are for the VSTest mode. (verified, source, Microsoft skill `migrate-xunit-to-xunit-v3`. The build check used neither: verified, probe, build)
- 4.0 adds Native AOT support, full test parallelization, class and method orderers, generic attributes such as `AssemblyFixtureAttribute<T>`, and fixture lifecycle interfaces (`INotifyTestLifecycleAsync` and others). (verified, source)
- xunit.analyzers 2.3.0 ships with 4.0.3. (verified, source)

### API that models must use

- `IAsyncLifetime` returns `ValueTask` and inherits from `IAsyncDisposable`. When `InitializeAsync` throws, xUnit does not call `DisposeAsync`. (verified, source, migration, 4.0.2 notes)
- `ITestOutputHelper` is in namespace `Xunit`. `Xunit.Abstractions` is gone. (verified, source)
- `async void` tests fail. Use `Task` or `ValueTask`. (verified, source)
- `TestContext.Current` gives the test, the output, the `CancellationToken`, and fixtures (`GetFixture<T>()`). Use it at once, do not store it. Pass `TestContext.Current.CancellationToken` to each async call. The analyzer xUnit1051 finds a missing token. (verified, source, [what's new](https://xunit.net/docs/getting-started/v3/whats-new))
- `Assert.Skip`, `Assert.SkipUnless`, `Assert.SkipWhen`. `[Fact(SkipUnless = nameof(Prop))]`. `[Fact(Explicit = true)]` runs only on request. (verified, source)
- Assembly fixtures: `[assembly: AssemblyFixture(typeof(DatabaseFixture))]`, injected in a test class constructor. (verified, source)
- Theory data: `TheoryData<T1, T2>`, `TheoryDataRow<T1, T2>` with `Skip`, `Label`, and display names, `MatrixTheoryData`, and async `MemberData`. (verified, source)
- `[assembly: CaptureConsole]` and `[assembly: CaptureTrace]` capture console and trace output. (verified, source)
- Parallel settings in 4.0: `[assembly: Parallelization(Mode = ParallelMode.None)]`, `MaxThreads`, and `Algorithm`, with the types `Xunit.v3.ParallelizationAttribute` and `Xunit.Sdk.ParallelMode`.
- In 4.0, `CollectionBehavior(DisableTestParallelization = ...)`, `MaxParallelThreads`, and `ParallelAlgorithm` are obsolete and cannot compile (CS0619). (verified, source, [running tests in parallel](https://xunit.net/docs/running-tests-in-parallel). (verified, probe, build)
- `Assert.All(collection, action, strict: true)` fails on an empty collection (4.0). (verified, source)

### Assertions and containers with xUnit v3

- FluentAssertions 7.1 and later detect xUnit v3 (assembly `xunit.v3.assert`). 7.2.2 does too. (verified, source, [7.1.0 release](https://github.com/fluentassertions/fluentassertions/releases/tag/7.1.0), `TestFrameworkProvider.cs` at tag 7.2.2)
- AwesomeAssertions detects xUnit v2, xUnit v3, MSTest 3, and MSTest 4. (verified, source, `TestFrameworkFactory.cs` on `main`)
- `Testcontainers.XunitV3` 4.17.0 needs `xunit.v3.extensibility.core >= 3.2.2`. It has `ContainerTest<TBuilder, TContainer>` (one container for each test), `ContainerFixture<TBuilder, TContainer>` (shared), and `DbContainerTest` and `DbContainerFixture` for ADO.NET. Share a fixture across the assembly with `[assembly: AssemblyFixture(...)]`. (verified, source, [xUnit.net docs of Testcontainers](https://dotnet.testcontainers.org/test_frameworks/xunit_net/))
- Testcontainers 4.10 and later need the image in the builder constructor. The skill already has this rule. (verified, source, `references/testcontainers.md`)

### Migration from v2

The Microsoft agent skill `migrate-xunit-to-xunit-v3` keeps the runner of the v2 project: VSTest with `IsTestingPlatformApplication=false`, or MTP when the project used `YTest.MTP.XUnit2`. (verified, source, [SKILL.md](https://github.com/dotnet/skills/blob/main/plugins/dotnet-test-migration/skills/migrate-xunit-to-xunit-v3/SKILL.md), last change 2026-10-09) It does not name the 4.0 parallel attributes. A migration to 4.x must also replace `CollectionBehavior` parallel properties.

## MSTest 4: deltas since 2026-10-27

Facts in `references/mstest-4.md` that change or are missing. Sources: [MSTest changelog](https://github.com/microsoft/testfx/blob/main/docs/Changelog.md), [MTP changelog](https://github.com/microsoft/testfx/blob/main/docs/Changelog-Platform.md), [v3 to v4 migration](https://learn.microsoft.com/dotnet/core/testing/unit-testing-mstest-migration-v3-v4) (verified, source).

| Item | Reference says | Current |
|------|----------------|---------|
| Version | MSTest 4.5.2, MTP 2.5.2 | MSTest 4.5.3, MTP 2.5.3 (2026-11-11). Bug fixes only: global cleanup after a timeout, `AddResultFile` after host shutdown, cancel during parallel runs |
| `MSTest.Sdk` and VSTest | Not named | 4.5.3 selects `Microsoft.NET.Test.Sdk` 18.10.2 when `UseVSTest` is true |
| Analyzers 0084 to 0089 (4.5.0) | Not listed | 0084 `OSCondition` vs `SupportedOSPlatform`. 0085 `[TestClass]` on an abstract class. 0086 method attribute that the class already gives. 0087 duplicate `DataRow` display names. 0088 hosted DI with AOT. 0089 `DisplayName` vs `ArgumentsDisplayName` conflict |
| MSTEST0014 | Not listed | `DataRow` argument types do not match the parameters. `[DataRow(100.00)]` for a `decimal` parameter gives only a warning at build. Then each row fails at run time: "Test data does not match method parameters" (verified, probe, build) |
| `DataRow` labels (4.5.0) | Not named | `ArgumentsDisplayName` on `DataRow` and `TestDataRow<T>` |
| DI in test classes (4.5.0) | Not named | `MSTest.Extensions.Hosting` (prerelease): one DI scope for each test |
| `TestContext.Properties` | `ContainsKey` | Since 4.4 the indexer returns `null` for a missing key. Since 4.3 values from `[AssemblyInitialize]` and `[ClassInitialize]` flow to the tests |
| Discovery | Not named | A type load error in the test assembly fails discovery in v4 (v3 gave a warning). This matters for Reqnroll #1126 |
| .NET 11 | Not named | 4.5.3 ships `net8.0` and `net9.0` assets, and a `net11.0` project uses them. No release note names .NET 11 for 4.x |
| MTP output for agents | Not named | MTP detects AI agent environments and shows less output there. `--show-stdout` and `--show-stderr` control it |

Microsoft has not released MSTest 5.0 and MTP 3.0. On 2026-11-20, `main` has these breaking changes:

- No .NET 8 and .NET 9 support. Only .NET 10, .NET 11, and .NET Framework.
- Microsoft removes `StringAssert`, `CollectionAssert`, and `[DataTestMethod]`.
- The analyzers need Roslyn 4.14.
- MTP removes `--no-ansi` and `--no-progress`.

 Main also adds `Assert.ContainsOnly`, `--partition`, `[ExpectedDuration]`, MSTEST0090 (`IQueryable` count), and a trailing `CancellationToken` parameter on a test method. The skill can warn now: no new `StringAssert` or `CollectionAssert` code.

## Recent AI mistakes

### Public reports (since 2026-05)

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| The Copilot skill `csharp-xunit` tells the agent to reference `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`. `xunit` is the v2 package. | github/awesome-copilot | Last change 2026-02-24, still the state on 2026-11-20 | [csharp-xunit SKILL.md](https://github.com/github/awesome-copilot/blob/main/skills/csharp-xunit/SKILL.md) | verified, source |
| A user followed "AI querying" and moved a test suite to .NET 10 to fix a Reqnroll discovery error. The cause was a version mismatch or an open bug on .NET Framework. | Unnamed AI tool | 2026-10-22 | [discussion #1125](https://github.com/reqnroll/Reqnroll/discussions/1125) | verified, source |
| The Reqnroll xUnit docs show `Xunit.Abstractions.ITestOutputHelper`. An agent that reads the docs writes the v2 namespace in a v3 project. | Reqnroll docs | Still the state on 2026-11-20 | [xUnit integration](https://docs.reqnroll.net/latest/integrations/xunit.html) | verified, source |
| LLMs make too few failure-path scenarios in Gherkin unless the prompt asks for them. | GPT-class models | 2026-07 | [arXiv 2607.01980](https://arxiv.org/pdf/2607.01980) | not verified (abstract only) |

No issue in reqnroll/Reqnroll, xunit/xunit, or microsoft/testfx after 2026-05-01 names a wrong test from an AI tool. The searches used "Copilot", "Claude", "LLM", "AI", and "AI generated". Reqnroll has a community MCP server for a static map of a suite (discussion #1104). This research did not read it.

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku 5.5 | rq1 | `Reqnroll.MSTest` 2.4.0 with `MSTest` 4.0.2. Reqnroll 2.x does not support MSTest 4. `Microsoft.NET.Test.Sdk` 17.14.1, no `MSTest.Sdk`. Domain classes inside the test project. `catch (Exception)` in the `When` step. Declarative steps: correct. |
| Sonnet 5.5 | rq1 | `Version="*"` for each package, no `MSTest.Sdk`, no `global.json`. Domain classes inside the test project. The feature is good: user story header, one `When` in each scenario, a `Scenario Outline` for the refused states. 3 `Then` steps in one scenario. |
| Haiku 5.5 | rq2 | `Reqnroll.MSTest` 2.4.0, `MSTest` 4.0.2, `Microsoft.Playwright` 1.56.0. Page and context in `ScenarioContext` string keys. Context per scenario, trace only on `TestError`, `CloseAsync` in `finally`: correct. Credentials from the environment: correct. |
| Sonnet 5.5 | rq2 | `Version="*"`, `MSTest.TestFramework` and `MSTest.TestAdapter` with VSTest. A password in the feature file (`"S3cret!pass"`). Injected session POCO, `Background`, trace only on failure, `Expect` with roles: correct. Parallel runs left off (comment only). |
| Haiku 5.5 | xu1 | `xunit.v3` 3.2.0 and `Microsoft.NET.Test.Sdk`. `ICollectionFixture` with `[Collection]`, not an assembly fixture. No `TestContext.Current.CancellationToken` (xUnit1051). `ValueTask` lifetime: correct. |
| Sonnet 5.5 | xu1 | `xunit.v3` 3.2.0, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk`: the VSTest path without `global.json`. `[assembly: AssemblyFixture]` and the cancellation token: correct. |
| Haiku 5.5 | xu2 | `[assembly: CollectionBehavior(DisableTestParallelization = true)]`: CS0619 on xUnit 4.0. `xunit.v3` 3.2.0. `TheoryData` and the token: correct. |
| Sonnet 5.5 | xu2 | Same `CollectionBehavior` error. `Version="*"`. `[InlineData]` with `double` for money values, cast in the test. |
| Haiku 5.5 | ms1 | `[DataRow(100.00, 10.0, 90.00)]` on `decimal` parameters: each row fails at run time (MSTEST0014). `MSTest` `4.*` and `Microsoft.NET.Test.Sdk` `17.*`. No `[assembly: Parallelize]`. `Assert.Throws<T>`: correct. |
| Sonnet 5.5 | ms1 | `MSTest` `4.*`, no `MSTest.Sdk`, no `global.json`, no `Parallelize`. `[DynamicData]` with tuples and `Assert.ThrowsExactly<T>`: correct. |

Both models know Reqnroll, not SpecFlow. Haiku knows only Reqnroll 2.4.0. Both models know `xunit.v3` 3.2.0 as the latest version and do not know 4.0. No model used `[ExpectedException]`, `[DataTestMethod]`, `Xunit.Abstractions`, or `IAsyncLifetime` with `Task`.

## New traps for the skills

| Skill | Trap | Current form | Mark | Source |
|-------|------|--------------|------|--------|
| reqnroll | `Reqnroll.MSTest` 2.x with MSTest 4 | Reqnroll 3.3.4 (3.2 or later for MSTest 4) | verified, probe | CHANGELOG 3.2.0, probe rq1, rq2 |
| reqnroll | `SpecFlow.*` packages, `TechTalk.SpecFlow`, `specflow.json`, SpecFlow+ LivingDoc | `Reqnroll.*`, `Reqnroll` namespace, `reqnroll.json` with schema, HTML formatter | verified, source | migration guide |
| reqnroll | `ScenarioContext.Current` or `ScenarioContext` string keys for shared state | Injected POCO (context injection) or `[ScenarioDependencies]` | verified, source | scenario context docs, probe rq2 |
| reqnroll | `Xunit.Abstractions.ITestOutputHelper` in a binding | `Xunit.ITestOutputHelper` with `Reqnroll.xunit.v3` | verified, probe | build check |
| reqnroll | `[BeforeFeature]` state with `MethodLevel` parallel runs | Class level, or no state in `FeatureContext` | verified, source | parallel execution docs |
| reqnroll | Browser context or page shared between scenarios | New context and page in `[BeforeScenario]`, close in `finally` | verified, source | Playwright docs |
| reqnroll | A trace file for each scenario | Save the trace only when `TestError` is not null | verified, source | Playwright docs |
| reqnroll | Password or token in a feature file | Read it from the environment in the step | verified, probe | probe rq2 |
| reqnroll | Imperative UI steps ("I click", "I type") | Declarative domain steps, UI in the step definition | verified, source | Cucumber better Gherkin |
| reqnroll | Many `When` steps, `Given` after `When` | One `When` for each scenario, Given-When-Then order | verified, source | Cucumber reference |
| reqnroll | Tags on `Examples` for MSTest filters | Tags on the scenario, or `allowRowTests: false` | verified, source | MSTest integration docs |
| reqnroll | Throw in `[AfterScenario]` | No throw in cleanup hooks (#1117 skips later hooks) | verified, source | #1117 |
| reqnroll | Test project named after a test framework | Set `<RootNamespace>` | verified, source | #1106 |
| dotnet-testing xunit | `xunit` 2.x, `Xunit.Abstractions`, `async void` | `xunit.v3` 4.0.3, `Xunit.ITestOutputHelper`, `Task` or `ValueTask` | verified, source | xUnit migration |
| dotnet-testing xunit | `xunit.v3` 3.x as "latest" | 4.0.3 (MTP v2 by default) | verified, probe | probes xu1, xu2 |
| dotnet-testing xunit | `CollectionBehavior(DisableTestParallelization = true)` or `MaxParallelThreads` | `[assembly: Parallelization(Mode = ParallelMode.None)]`, `MaxThreads` | verified, probe | build check, probes xu2 |
| dotnet-testing xunit | `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` with no `global.json` on SDK 10 | `global.json` MTP runner, no VSTest packages. Or `xunit.v3.mtp-off` for VSTest | verified, source | 4.0.0 notes |
| dotnet-testing xunit | `IAsyncLifetime` with `Task` | `ValueTask` | verified, source | xUnit migration |
| dotnet-testing xunit | No token in async calls | `TestContext.Current.CancellationToken` (xUnit1051) | verified, probe | probe xu1 |
| dotnet-testing xunit | `ICollectionFixture` for one container in the whole assembly | `[assembly: AssemblyFixture(typeof(...))]` or `ContainerFixture` from `Testcontainers.XunitV3` | verified, source | Testcontainers docs |
| dotnet-testing xunit | `Skip = "..."` for a condition | `Assert.SkipUnless`, `SkipWhen`, or `Explicit = true` | verified, source | xUnit "What's New in v3" page |
| dotnet-testing mstest | `[DataRow(100.00)]` for a `decimal` parameter | `double` parameter with a cast, or `[DynamicData]` (MSTEST0014) | verified, probe | build check, probe ms1 |
| dotnet-testing mstest | `MSTest` `4.*` or `Version="*"` | `MSTest.Sdk` 4.5.3 pinned in `global.json` | verified, probe | probes ms1, rq1 |
| dotnet-testing mstest | No `[assembly: Parallelize]` | `Parallelize` in each test assembly (MSTEST0001) | verified, probe | probe ms1 |
| dotnet-testing mstest | New `StringAssert` or `CollectionAssert` code | `Assert` APIs (removed in MSTest 5) | verified, source | testfx changelog |
| dotnet-testing mstest | MSTest and xUnit projects in one repository on different runners | One `global.json` runner mode for each repository | verified, source | xUnit 4.0.0 notes, `mstest-4.md` |

## Facts that are not verified

- Reqnroll support for .NET 11. The compatibility page stops at .NET 10. The research did no build on the .NET 11 SDK. Only SDK 10.0.404 is on the research machine.
- MSTest 4.5.3 and xUnit v3 4.0.3 on the .NET 11 SDK and on `net11.0`.
- Whether a Reqnroll binding can use the `PageTest` base classes of `Microsoft.Playwright.MSTest.v4`.
- Reqnroll with `xunit.v3` 4.0 parallel modes (`ParallelMode.All`) and the `addNonParallelizableMarkerForTags` setting. The build check ran only `ParallelMode.None` and the default.
- The date of a Reqnroll 4.0 release. The DI plugin limits Reqnroll to `[3.3.4, 4.0.0)`, so a 4.0 release will need a new plugin version.
- Whether Reqnroll Cucumber expressions accept `{decimal}`. Sonnet used it in probe rq1. The build check did not test it.
- The arXiv study on failure-path scenarios. The research read only the search abstract.
- Reports of AI mistakes on Reddit or Stack Overflow in this window. The web search returned none. Reddit was not searched directly.
- The probe answers were not compiled, except the `CollectionBehavior` form and the `DataRow` decimal form, which the build check copied.
