# Playwright in Reqnroll bindings

Reqnroll 3.3.4 with `Microsoft.Playwright` 1.65.1 and `WebApplicationFactory`, on .NET 10 and .NET 11. The rule numbers refer to `SKILL.md`. A compile check built and ran this code with `Reqnroll.MsTest` on `MSTest.Sdk` 4.5.3, on `net10.0` and on `net11.0` (`sources.md`). Locators, assertions, and the browser install are in `test-stack:playwright`.

## Contents

- Why own hooks
- Lifecycle
- The session POCO
- Hooks
- Step definitions
- The app under test
- Trace on failure
- Parallel runs

## Why own hooks

Reqnroll generates the test class of each feature. A binding class is not a test class, so it cannot inherit `PageTest` from `Microsoft.Playwright.MSTest.v4`. The hooks below do the work of `PageTest` (RQ-02). Reference `Microsoft.Playwright`, not an adapter package.

## Lifecycle

| Object | Life | Hook |
|--------|------|------|
| `WebApplicationFactory<Program>` with Kestrel | One test run | static `[BeforeTestRun]`, `[AfterTestRun]` |
| `IPlaywright`, `IBrowser` | One test run | static `[BeforeTestRun]`, `[AfterTestRun]` |
| `IBrowserContext`, `IPage`, the trace | One scenario | `[BeforeScenario]`, `[AfterScenario]` |

A new context for each scenario gives each scenario empty cookies and storage. A browser start is slow, so the browser starts once.

## The session POCO

Reqnroll makes one `BrowserSession` for each scenario and gives the same instance to each binding class that asks for it (context injection, rule 8). Do not keep the page in `ScenarioContext` string keys.

```csharp
using Microsoft.Playwright;

public sealed class BrowserSession
{
    public IBrowserContext Context { get; set; } = null!;
    public IPage Page { get; set; } = null!;
}
```

## Hooks

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Reqnroll;

[Binding]
public sealed class Hooks(BrowserSession session, ScenarioContext scenario)
{
    private static WebApplicationFactory<Program> factory = null!;
    private static IPlaywright playwright = null!;
    private static IBrowser browser = null!;
    public static Uri BaseAddress { get; private set; } = null!;

    [BeforeTestRun]
    public static async Task StartAsync()
    {
        factory = new WebApplicationFactory<Program>();
        factory.UseKestrel(0);
        factory.StartServer();
        BaseAddress = factory.ClientOptions.BaseAddress;
        playwright = await Playwright.CreateAsync();
        browser = await playwright.Chromium.LaunchAsync();
    }

    [AfterTestRun]
    public static async Task StopAsync()
    {
        await browser.DisposeAsync();
        playwright.Dispose();
        await factory.DisposeAsync();
    }

    [BeforeScenario]
    public async Task OpenContextAsync()
    {
        session.Context = await browser.NewContextAsync(new() { BaseURL = BaseAddress.ToString() });
        await session.Context.Tracing.StartAsync(new() { Screenshots = true, Snapshots = true, Sources = true });
        session.Page = await session.Context.NewPageAsync();
    }

    [AfterScenario]
    public async Task CloseContextAsync()
    {
        try
        {
            var failed = scenario.TestError is not null;
            await session.Context.Tracing.StopAsync(failed
                ? new() { Path = Path.Combine("traces", $"{scenario.ScenarioInfo.Title}.zip") }
                : new());
        }
        finally
        {
            await session.Context.CloseAsync();
        }
    }
}
```

- The `[AfterScenario]` hook does not throw. It closes the context in a `finally`. When an `[AfterScenario]` hook throws, Reqnroll skips the later `[AfterScenario]` hooks (#1117). A scenario can also go missing from the HTML report after a cleanup error (#1083).
- Since Reqnroll 3.2.1, `[AfterScenario]` also runs for a scenario that ends as skipped or inconclusive.
- For a hook only for browser scenarios, put `[Scope(Tag = "web")]` on the hook and `@web` on the features.

## Step definitions

```csharp
using Microsoft.Playwright;
using Reqnroll;
using static Microsoft.Playwright.Assertions;

[Binding]
public sealed class OrderSteps(BrowserSession session, OrderState order)
{
    [When("the user opens the home page")]
    public async Task OpenHomeAsync() => await session.Page.GotoAsync("/");

    [Then("the user sees the heading {string}")]
    public async Task SeeHeadingAsync(string name) =>
        await Expect(session.Page.GetByRole(AriaRole.Heading, new() { Name = name })).ToBeVisibleAsync();

    [Given("an order of {decimal} euro")]
    public void GivenOrder(decimal amount) => order.Total = amount;

    [When("a discount of {decimal} euro is applied")]
    public void ApplyDiscount(decimal discount) => order.Total -= discount;

    [Then("the total is {decimal} euro")]
    public void CheckTotal(decimal expected) => Assert.AreEqual(expected, order.Total);
}
```

`OrderState` is a POCO with one property `decimal Total`. `Assert.AreEqual` is the MSTest assertion. With xUnit v3, use `Assert.Equal`.

- `{decimal}` works in a Cucumber expression in Reqnroll 3.3.4 (MSTest and xUnit v3). `100.00 - 10.50` gives `89.50`.
- The step opens `"/"`. The context has the `BaseURL`, so the step needs no address.
- Use `GetByRole`, `GetByLabel`, and `Expect(...)` (rule 15). A check on `IsVisibleAsync()` does not wait.
- For a sign-in, the step reads the test account from environment variables (rule 7).

## The app under test

- `UseKestrel(0)` selects Kestrel on a free port. `StartServer()` starts it. Both exist on .NET 10 and later.
- After `StartServer()`, `factory.ClientOptions.BaseAddress` is the real address, with a slash at the end (`http://127.0.0.1:<port>/`). `CreateClient().BaseAddress` gives the same address for an API step.
- `public partial class Program` is not necessary on .NET 10 and .NET 11. `WebApplicationFactory<Program>` compiles without it.
- A browser cannot reach the in-memory `TestServer`. Do not build a second Kestrel host in `CreateHost`.
- The app keeps one database for the run. Each scenario makes its own data. For a database container and a reset, read `test-stack:dotnet-testing`.

## Trace on failure

- The trace starts with `Screenshots`, `Snapshots`, and `Sources` for each scenario.
- `StopAsync` with a `Path` saves the file. `StopAsync` without a `Path` discards it. Only a failed scenario gets a file.
- The relative path `traces/<title>.zip` is relative to `bin/Debug/<tfm>/`. In CI, upload this folder as an artifact when the run fails.
- Open a trace in the Playwright trace viewer. `test-stack:playwright` tells how.

## Parallel runs

- MSTest runs features (`ExecutionScope.ClassLevel`) or scenarios (`ExecutionScope.MethodLevel`) in parallel. xUnit runs only features in parallel.
- With `MethodLevel`, `[BeforeFeature]`, `[AfterFeature]`, and `FeatureContext` run once for each feature and each thread. Keep no state in `FeatureContext`, or use `ClassLevel` (RQ-06).
- The browser is static and each scenario gets its own context, so the hooks do not share a page between scenarios.
- The compile check ran this code without a parallel setting. Run the suite 3 times after you turn on parallel runs.
