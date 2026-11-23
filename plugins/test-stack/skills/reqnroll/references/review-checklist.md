# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Packages and setup
- Feature files
- Bindings and hooks
- Browser steps
- Severity
- Do not flag

## Packages and setup

| # | Check | Type |
|---|-------|------|
| 1 | Reqnroll 3.3.4. With MSTest 4, no Reqnroll 2.x. | tool (RQ-PKG-OLD) |
| 2 | No `SpecFlow.*` package, no `TechTalk.SpecFlow` namespace, no `specflow.json`. | tool (RQ-SPECFLOW-NAMESPACE), judge |
| 3 | xUnit v3 uses `Reqnroll.xunit.v3`, not `Reqnroll.xUnit`. No `Xunit.Abstractions`. | judge |
| 4 | Each Reqnroll package has one pinned version. No `*`. | judge |
| 5 | MSTest on `MSTest.Sdk`, without `Microsoft.NET.Test.Sdk` or `MSTest.TestAdapter`. | judge |
| 6 | `global.json` has `"test": { "runner": "Microsoft.Testing.Platform" }`. | judge |
| 7 | CI builds first and then runs `dotnet test --no-build`. | judge |
| 8 | The project sets `<RootNamespace>` when its name contains the name of a test framework. | judge |
| 9 | `reqnroll.json` has the `$schema` line. | judge |

## Feature files

| # | Check | Type |
|---|-------|------|
| 10 | Steps are declarative. No "I click", "I type", or field names. | tool (RQ-IMPERATIVE-STEP), judge |
| 11 | One `When` in each scenario. Given, When, Then in this order. | judge |
| 12 | Each `Then` checks a result that the user or an external system can see. | judge |
| 13 | `Background` holds only `Given` steps that each scenario needs. | judge |
| 14 | Each rule has a failure-path scenario. | judge |
| 15 | No password, token, or real personal data. | tool (RQ-SECRET-IN-FEATURE), judge |
| 16 | With MSTest, no tag on an `Examples` block, or `allowRowTests` is `false`. | judge |
| 17 | The language follows RQ-04. A Dutch file has `# language: nl`. | judge |
| 18 | New steps reuse existing step texts and use parameters. No step for each screen. | judge |

## Bindings and hooks

| # | Check | Type |
|---|-------|------|
| 19 | No `ScenarioContext.Current` or `FeatureContext.Current`. | tool (RQ-CONTEXT-CURRENT) |
| 20 | No string keys in `ScenarioContext`. State is in an injected POCO. | tool (RQ-CONTEXT-STRING-KEY), judge |
| 21 | `[BeforeTestRun]`, `[AfterTestRun]`, `[BeforeFeature]`, `[AfterFeature]` are static. | judge |
| 22 | No throw in an `[AfterScenario]` hook. Each close is in a `finally`. | judge |
| 23 | Two hooks of one type that depend on their order set `Order`. | judge |
| 24 | No `[Scope(Feature = ...)]` on a step definition. | judge |
| 25 | With `MethodLevel` parallel runs, no state in `FeatureContext`. | judge |

## Browser steps

| # | Check | Type |
|---|-------|------|
| 26 | The browser starts once in `[BeforeTestRun]`. Each scenario gets a new context and page. | judge |
| 27 | The hook saves the trace only when `TestError` is not null. | judge |
| 28 | The context has `BaseURL` from `factory.ClientOptions.BaseAddress`. The app runs through `UseKestrel(0)` and `StartServer()`. | judge |
| 29 | Locators by role or label, `Expect(...)` assertions, no fixed waits (`test-stack:playwright`). | judge |
| 30 | A sign-in step reads the test account from the environment. | judge |

## Severity

These findings are blocking:

- A password, a token, or real personal data in a feature file (15).
- Reqnroll 2.x with MSTest 4 (1).
- A context or page that two scenarios share (26).
- A throw in an `[AfterScenario]` hook that leaves the context open (22).

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `ScenarioContext` injected in a hook to read `TestError` or `ScenarioInfo`. Only string keys are a finding.
- A UI word in a step when the UI is the behaviour, for example "Then the cancel button is not available".
- `Reqnroll.xUnit` and `Xunit.Abstractions` in a project on xUnit v2.
- More than one `Then` in a scenario, when the steps check the result of the one `When`.
- `[Scope(Tag = "web")]` on a hook.
- A feature file in Dutch with `# language: nl`, with step definitions in English code.
