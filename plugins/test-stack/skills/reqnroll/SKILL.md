---
name: reqnroll
description: Acceptance tests with Reqnroll 3 (BDD, Gherkin) on MSTest 4 or xUnit v3, .NET 10 and .NET 11. Covers the packages and versions, global.json for Microsoft.Testing.Platform, reqnroll.json, declarative Gherkin (one When, Rule, Background, Scenario Outline, tags, language), step definitions with Cucumber expressions, context injection instead of ScenarioContext keys, hooks, Playwright in the bindings (browser for each run, context and page for each scenario, trace on failure), WebApplicationFactory with UseKestrel, parallel runs, the HTML formatter, and the move from SpecFlow. Use when code or a review touches Reqnroll, Gherkin, .feature files, step definitions, [Binding], hooks, BDD acceptance tests, reqnroll.json, or a move from SpecFlow. Not for plain Playwright tests (test-stack:playwright), .NET unit and API tests (test-stack:dotnet-testing), or the acceptance criteria and Behaviour spec of a backlog item (product-flow).
metadata:
  verified: "2026-11-21"
  versions: "Reqnroll 3.3.4 (Reqnroll.MsTest, Reqnroll.xunit.v3), MSTest.Sdk 4.5.3, xunit.v3 4.0.3, Microsoft.Playwright 1.65.1, Microsoft.AspNetCore.Mvc.Testing 10.0.15 and 11.0.0, .NET 10 and .NET 11 SDK"
---

# Reqnroll

Rules for acceptance tests with Reqnroll: the `.feature` files in Gherkin, the step definitions, the hooks, and the test project. A feature file tells the behaviour in the words of the business. The step definitions do the work, also through a browser with Playwright.

Other skills own the parts around the features:

- Playwright tests without Gherkin, locators, and the browser install: `test-stack:playwright`.
- .NET unit tests, API tests, Testcontainers, and the MSTest or xUnit rules that are not about Reqnroll: `test-stack:dotnet-testing`.
- The acceptance criteria and the Behaviour spec of a backlog item: `product-flow`. This skill turns an approved spec into feature files and step definitions.
- Secrets and test accounts in general: `dev-stack:secure-coding`.

## First

1. Read `references/stack-rules.md`. It has the common rules of all stack skills.
2. Find the versions in `*.csproj`, `Directory.Packages.props`, and `global.json`. Done when you can name each one:
   - Reqnroll: `Reqnroll.MsTest`, `Reqnroll.xunit.v3`, or `Reqnroll.NUnit`. 3.3.4 is current. With MSTest 4, Reqnroll 3.2 or later is necessary. For 2.x or SpecFlow, read "Version gate".
   - Test framework: `MSTest.Sdk` 4.x or `xunit.v3` 4.x. The framework of the repository stays (RQ-01).
   - SDK: the `sdk` and `test` sections of `global.json`.
3. Find the existing features: the language header, the step texts, and the hooks. New steps reuse the existing domain words (rule 6). Done when you have the list of step texts that the new scenario can reuse.
4. For a UI scenario, find the app under test: the web project that `WebApplicationFactory<Program>` starts. Done when you know how the browser gets a running app.

## Version gate

Existing code keeps its versions until the task asks for an upgrade (stack rule 3). These versions need a change before other work:

- `Reqnroll.MsTest` 2.x with MSTest 4: Reqnroll 2.x does not support MSTest 4. Upgrade Reqnroll to 3.3.4, or keep MSTest 3.
- `Reqnroll.xUnit` is for xUnit v2. xUnit v3 needs `Reqnroll.xunit.v3` (Reqnroll 3.1 or later).
- SpecFlow reached its end of life on 2024-12-31. Read the move table in `references/setup.md`.
- Since 3.3.0, `NotImplementedException` in a step is a failure, not "pending".
- Since 3.0.0, the step attribute property `Regex` is `Expression`.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The owner approved RQ-01 to RQ-04 as catalog defaults (P3-03, P3-05 to P3-07). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Test framework | `Reqnroll.MsTest` on `MSTest.Sdk` 4.5.3. `Reqnroll.xunit.v3` where the repository uses xUnit v3 | RQ-01 |
| Browser in the steps | Own hooks: browser once for each run, a new context and page for each scenario, in an injected POCO. No `PageTest` | RQ-02 |
| App under test | `WebApplicationFactory<Program>` with `UseKestrel(0)` and `StartServer()` in `[BeforeTestRun]` | RQ-03 |
| Feature language | The language of the existing features. English in a new repository. Dutch with `# language: nl` when the product owner writes Dutch | RQ-04 |
| Shared state | Context injection of a POCO. `Reqnroll.Microsoft.Extensions.DependencyInjection` only when the bindings need app services | RQ-05 |
| Parallel runs | MSTest: features in parallel (`ClassLevel`). Scenarios in parallel only with no `FeatureContext` state | RQ-06 |
| Report | The HTML formatter in CI | RQ-07 |

## Rules

### Gherkin

1. Write declarative steps in domain words: "When the customer cancels the order", not "When I click the Cancel button". UI details go into the step definition. A step with a click does not survive a change of the screen, and the product owner cannot read it.
2. Give each scenario one behaviour: one `When`, then the `Then` steps for its result. Keep the order Given, When, Then. Two `When` steps are two scenarios.
3. A `Then` checks a result that the user or an external system can see, not a database row.
4. Use `Background` only for `Given` steps that each scenario of the feature or the `Rule` needs. Group the scenarios of one business rule under `Rule`. Use `Scenario Outline` with `Examples` when only the data changes.
5. Write a failure-path scenario for each rule: the refused cancel, the wrong input, the missing right. Models write too few of them.
6. Reuse steps through parameters (`{string}`, `{int}`, `{decimal}`). One step for each screen gives many near-copies of one step. Do not couple a step to one feature with `[Scope(Feature = ...)]`.
7. Keep passwords, tokens, and real personal data out of feature files. The step definition reads the secret from the environment.

Read `references/gherkin.md` before you write or review a feature file. It has the full rules, the Dutch keywords, and an example before and after.

### Bindings and hooks

8. Share state through context injection. A POCO in the constructor of a binding class gets one instance for each scenario, shared by each binding class. `ScenarioContext.Current` is obsolete and fails in parallel runs. String keys in `ScenarioContext` give no type check and fail only at run time.
9. Make `[BeforeTestRun]`, `[AfterTestRun]`, `[BeforeFeature]`, and `[AfterFeature]` static. Hooks of one type run in an order that you cannot predict. Set `Order` when the order is important.
10. Do not throw from an `[AfterScenario]` hook. When one `[AfterScenario]` throws, Reqnroll skips the later ones (issue #1117), and resources stay open. Close resources in a `finally`.
11. With xUnit v3, inject `Xunit.ITestOutputHelper`. The Reqnroll docs show `Xunit.Abstractions.ITestOutputHelper`, which is the xUnit v2 type.

### Playwright in the steps

12. Start `IPlaywright` and `IBrowser` once in `[BeforeTestRun]`. Make a new `IBrowserContext` and `IPage` for each scenario in `[BeforeScenario]`, and keep them in an injected `BrowserSession` POCO (RQ-02). A context that two scenarios share leaks cookies and storage from one scenario into the next.
13. Start the trace in `[BeforeScenario]`. In `[AfterScenario]`, save it only when `ScenarioContext.TestError` is not null, and close the context in a `finally`.
14. Set `BaseURL` on the context from `factory.ClientOptions.BaseAddress` after `StartServer()` (RQ-03). Then a step opens `"/"` and the address has one source.
15. Use role and label locators and `Expect(...)` web-first assertions, as in `test-stack:playwright` rules 1 to 3.

Read `references/playwright-bindings.md` before you write hooks or browser steps. It has the checked code.

### Project and run

16. Use Reqnroll 3.3.4. Put the MTP runner in `global.json` on SDK 10 and SDK 11. Without it, `dotnet test` stops with a VSTest error.
17. Build first, then run `dotnet test --no-build`. In MTP mode, `dotnet test` gives MSBuild switches such as `--disable-build-servers` to the test app, which stops with "Unknown option".
18. With MSTest, put a tag on the scenario, not on an `Examples` block. MSTest does not give the tags of an `Examples` block to the runner.
19. Set `<RootNamespace>` when the project name or namespace contains the name of a test framework. Else the compiler can drop `Reqnroll.dll` and gives CS0246 on `[Binding]` (#1106).

Read `references/setup.md` before you make or change a test project, `reqnroll.json`, or a CI job, and before a move from SpecFlow.

## Never write

| Wrong | Correct |
|-------|---------|
| `<PackageReference Include="Reqnroll.MsTest" Version="2.4.0" />` with MSTest 4 | `Reqnroll.MsTest` 3.3.4 |
| `using TechTalk.SpecFlow;` | `using Reqnroll;` |
| `ScenarioContext.Current["page"]` | An injected `BrowserSession` POCO |
| `scenarioContext["page"] = page;` and `(IPage)scenarioContext["page"]` | `session.Page = page;` in the hook, `session.Page` in the step |
| `using Xunit.Abstractions;` with `Reqnroll.xunit.v3` | `using Xunit;` (`Xunit.ITestOutputHelper`) |
| `When I click the "Cancel" button` | `When the customer cancels the order` |
| `And I enter the password "S3cret!pass"` | `Given the customer has signed in` (the step reads the secret from the environment) |
| `When ... And when ...` in one scenario | One `When` for each scenario |
| `@smoke` above `Examples:` with MSTest | `@smoke` above the `Scenario Outline` |
| `throw` in an `[AfterScenario]` hook | Cleanup in `try` and `finally`, no throw |
| `Version="*"` for a Reqnroll package | A pinned version |

## Check

1. Build, then run the tests:

   ```sh
   dotnet build
   dotnet test --no-build
   ```

   Pass: the summary shows 0 failed. When you added or changed a scenario with a browser, run it 3 times. Pass: 3 times green.
2. Find steps without a binding:

   ```sh
   REQNROLL_DRY_RUN=true dotnet test --no-build
   ```

   Pass: no undefined step.
3. Run the rules of this skill:

   ```sh
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `# stackcheck: ignore <RULE-ID> <reason>` (feature file) or `// stackcheck: ignore <RULE-ID> <reason>` (C#).
4. Classify each red scenario before you change it. The classes are: a defect in the app, a defect in the step definition, or a flaky scenario. Done when the report names the class and the proof (trace, log line). A real regression stays red.

## Review checklist

The full list is in `references/review-checklist.md`.

1. Reqnroll 3.3.4 with a pinned version, and the correct framework package. tool (RQ-PKG-OLD), judge
2. No SpecFlow names. tool (RQ-SPECFLOW-NAMESPACE)
3. State in an injected POCO, no `ScenarioContext.Current` or string keys. tool (RQ-CONTEXT-CURRENT, RQ-CONTEXT-STRING-KEY)
4. Declarative steps, one `When`, Given-When-Then order. tool (RQ-IMPERATIVE-STEP), judge
5. No secret in a feature file. tool (RQ-SECRET-IN-FEATURE), judge
6. A new context and page for each scenario, a trace only on failure, no throw in `[AfterScenario]`. judge
7. A failure-path scenario for each rule. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Checks": the test run, the dry run, and the class of each red scenario.
- Under "Packages": the Reqnroll version and the framework package before and after.
- Under "Decisions": each RQ decision that the change uses. Mark each one accepted or assumed.

## References

- `references/gherkin.md`: read before you write or review a feature file.
- `references/playwright-bindings.md`: read before you write hooks or steps that use a browser.
- `references/setup.md`: read before you make or change a test project, `reqnroll.json`, or CI, or move from SpecFlow.
- `references/decisions.md`: read when new code needs a decision with an ID from the defaults table.
- `references/review-checklist.md`: read before a full review.
- `references/sources.md`: the primary sources, the date of each check, and the facts that are not verified.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
