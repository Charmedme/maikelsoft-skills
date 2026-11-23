# Reqnroll decisions

The team decisions that acceptance tests with Reqnroll need. The owner approved RQ-01 to RQ-04 as catalog defaults (P3-06, P3-05, P3-03, P3-07). RQ-05 to RQ-07 are recommendations. Ask the human before you apply a recommendation that has no accepted decision (stack rule 4).

## Contents

- RQ-01. Test framework
- RQ-02. Browser in the steps
- RQ-03. App under test
- RQ-04. Feature language
- RQ-05. Shared state
- RQ-06. Parallel runs
- RQ-07. Report

## RQ-01. Test framework

- Question: Which test framework runs the Reqnroll scenarios?
- Ask when: a new acceptance test project.
- Options: `Reqnroll.MsTest` on `MSTest.Sdk` 4.x. `Reqnroll.xunit.v3` on `xunit.v3` 4.x. `Reqnroll.NUnit`. `Reqnroll.TUnit`.
- Recommended: The framework of the other test projects of the repository. MSTest 4 in a new repository (TEST-01 in `test-stack:dotnet-testing`). Do not move a repository to another framework unless the task asks.
- Choose another option when: the repository uses NUnit or TUnit. Then the Reqnroll package for it.
- Depends on: TEST-01.
- Revisit when: MSTest 5 ships, or Reqnroll 4 ships.
- Check: tool (RQ-PKG-OLD), judge.
- Detect: DET-RQ-01.

## RQ-02. Browser in the steps

- Question: How do the step definitions get a browser page?
- Ask when: the first scenario with a browser.
- Options: Own hooks with `Microsoft.Playwright`: the browser once for each run, a new context and page for each scenario, kept in an injected POCO. A `PageTest` base class of a Playwright adapter. Page and context in `ScenarioContext` keys.
- Recommended: Own hooks with an injected `BrowserSession` POCO (`playwright-bindings.md`). Reqnroll generates the test class, so a binding class cannot use `PageTest`. A POCO gives a type check, and string keys do not.
- Choose another option when: never for new code. Existing code with `ScenarioContext` keys keeps them until the task touches the hooks.
- Depends on: RQ-05.
- Revisit when: Reqnroll or Playwright ships an official integration.
- Check: tool (RQ-CONTEXT-STRING-KEY), judge.
- Detect: DET-RQ-02.

## RQ-03. App under test

- Question: How does a browser scenario get a running app?
- Ask when: the first scenario with a browser.
- Options: `WebApplicationFactory<Program>` with `UseKestrel(0)` and `StartServer()` in `[BeforeTestRun]`. `Aspire.Hosting.Testing` with the AppHost. A deployed URL.
- Recommended: `WebApplicationFactory` with Kestrel for an app in the same repository (PW-03 in `test-stack:playwright`). The test can replace services, and the port is free. `Aspire.Hosting.Testing` when the repository has an AppHost. A deployed URL only for a smoke test.
- Choose another option when: the frontend is a separate app that the .NET host does not serve.
- Depends on: PW-03.
- Revisit when: the app gets an Aspire AppHost.
- Check: judge.
- Detect: none.

## RQ-04. Feature language

- Question: In which language are the feature files?
- Ask when: the first feature file of a repository, or when the product owner writes specs in another language.
- Options: English. Dutch with `# language: nl`. Another Gherkin language.
- Recommended: The language of the existing features. English in a new repository. Dutch with `# language: nl` when the product owner writes the specs in Dutch. Step definitions and code stay in English.
- Choose another option when: the product owner and the testers read another language.
- Depends on: none.
- Revisit when: the product owner changes.
- Check: judge.
- Detect: DET-RQ-04.

## RQ-05. Shared state

- Question: How do binding classes share state in a scenario?
- Ask when: the first binding class that needs data from another binding class, or an app service.
- Options: Context injection of POCO classes (built in). `Reqnroll.Microsoft.Extensions.DependencyInjection` with a static `[ScenarioDependencies]` method that returns an `IServiceCollection`. `ScenarioContext` keys.
- Recommended: Context injection of POCO classes. It needs no package. Add the DI plugin only when the bindings need services of the app or a typed `HttpClient` from a service collection. The plugin needs the same Reqnroll version (`[3.3.4, 4.0.0)`).
- Choose another option when: the project already uses the DI plugin.
- Depends on: none.
- Revisit when: Reqnroll 4 ships, because the plugin must change with it.
- Check: tool (RQ-CONTEXT-CURRENT, RQ-CONTEXT-STRING-KEY), judge.
- Detect: none.

## RQ-06. Parallel runs

- Question: Do features or scenarios run in parallel?
- Ask when: a new acceptance test project, or the run takes too long.
- Options: No parallel runs. Features in parallel (`[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]`). Scenarios in parallel (`ExecutionScope.MethodLevel`).
- Recommended: Features in parallel with MSTest. Each scenario makes its own data. Scenarios in parallel only when no binding keeps state in `FeatureContext` or `[BeforeFeature]`, because these run once for each feature and each thread. Mark features that must run alone with a tag in `generator/addNonParallelizableMarkerForTags`. xUnit runs only features in parallel.
- Choose another option when: the scenarios share data that a reset cannot split. Then no parallel runs, until each scenario has its own data.
- Depends on: RQ-01.
- Revisit when: the run time is too long, or a scenario fails only in parallel runs.
- Check: judge.
- Detect: none.

## RQ-07. Report

- Question: Which report does CI keep of the acceptance tests?
- Ask when: a new CI job for the acceptance tests, or a move from SpecFlow+ LivingDoc.
- Options: The Reqnroll HTML formatter. The `message` formatter for another tool. Only the test results of the runner.
- Recommended: The HTML formatter, uploaded as a CI artifact. It replaces LivingDoc, and the product owner can read it.
- Choose another option when: the team has a Cucumber reporting tool. Then the `message` formatter.
- Depends on: none.
- Revisit when: a scenario goes missing from the report (#1083).
- Check: judge.
- Detect: none.
