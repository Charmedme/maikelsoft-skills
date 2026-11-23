# Compile check: phase 3 of the stack split

Date: 2026-11-21. Status: done. This is decision P3-08 of `concept-phase3.md`. The check compiles and runs the facts that `research-playwright.md` and `research-reqnroll-xunit-mstest.md` mark "not verified". A skill rule can use a claim only when this file marks it "pass".

## Contents

- Setup
- Playwright with MSTest 4
- Playwright with xUnit v3
- WebApplicationFactory with Kestrel
- Reqnroll with MSTest and Playwright
- Reqnroll with xUnit v3
- xUnit v3 assembly attributes and global.json
- MSTest DataRow with decimal
- .NET 11
- Corrections to the research

## Setup

- SDKs: the system SDK 10.0.404 for `net10.0`, and SDK 11.0.100 in a scratch folder for `net11.0`. Each folder has its own `global.json`.
- Packages: `MSTest.Sdk` 4.5.3, `xunit.v3` 4.0.3, `Microsoft.Playwright`, `Microsoft.Playwright.MSTest.v4`, and `Microsoft.Playwright.Xunit.v3` 1.65.1, `Reqnroll.MSTest` and `Reqnroll.xunit.v3` 3.3.4, `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 (on `net10.0`) and 11.0.0 (on `net11.0`).
- Build command: `dotnet build --disable-build-servers -p:UseSharedCompilation=false -nodeReuse:false`, then `dotnet build-server shutdown`. One build or test run at a time.
- Test command: `dotnet test --no-build --project <name>`. In MTP mode, `dotnet test` sends MSBuild switches to the test app. `dotnet test --disable-build-servers` gives `Unknown option '--disable-build-servers'` and exit code 5. Thus the check builds first and then runs `dotnet test --no-build`.
- `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, and `PLAYWRIGHT_BROWSERS_PATH` point to the scratch folder. The browser cache of the user did not change.
- No Docker, no database. The web app is a minimal API with one HTML page.
- The solution has one web project (`Web`, `Microsoft.NET.Sdk.Web`) and the test projects. Each test project has a `ProjectReference` to `Web` when it uses `WebApplicationFactory<Program>`.

## Playwright with MSTest 4

Code: `Sdk="MSTest.Sdk/4.5.3"` with a `PackageReference` to `Microsoft.Playwright.MSTest.v4` 1.65.1. No other property.

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

[TestClass]
public class LocalPageTests : PageTest
{
    [TestMethod]
    public async Task Heading_is_visible()
    {
        await Page.SetContentAsync("<h1>Orders</h1>");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Orders" })).ToBeVisibleAsync();
    }
}
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `Microsoft.Playwright.MSTest.v4` 1.65.1 with `MSTest.Sdk` 4.5.3 and MTP compiles | pass | Build succeeded, 0 warnings. No extra property. |
| A `PageTest` test runs | pass | `dotnet test`: `total: 2, failed: 0, succeeded: 2` (with the Kestrel test of the next sections). |
| Browser install with `pwsh bin/Debug/net10.0/playwright.ps1 install` | not possible | The machine has no `pwsh`. The build writes `playwright.ps1`, which needs PowerShell. |
| Browser install with the bundled Node.js driver | pass | The next command installed Chromium 159 (revision 1255), the headless shell, and FFmpeg. On CachyOS it gives "BEWARE: your OS is not officially supported" and gets the Ubuntu 24.04 build. That build runs. |
| The plain `Microsoft.Playwright.MSTest` 1.65.1 fails with MSTest 4 | pass, but not as a build error | The build succeeds with 7 warnings MSTEST0082. The run finds 0 tests: "Zero tests ran", exit code 8. `--list-tests` gives "found 0 test(s)". |

This command installs Chromium without PowerShell. Run it in the project folder after the build:

```sh
bin/Debug/net10.0/.playwright/node/linux-x64/node bin/Debug/net10.0/.playwright/package/cli.js install chromium
```

The MSTEST0082 text for the plain package (one of 7 lines):

```text
warning MSTEST0082: The inherited member 'PageTest.PageSetup' is annotated with a '[TestInitialize]' attribute that resolves to a canonical MSTest attribute in 'Microsoft.VisualStudio.TestPlatform.TestFramework', a different MSTest framework assembly than this test project uses; MSTest matches these attributes by exact type identity, so the inherited '[TestInitialize]' is silently ignored at run time.
```

The plain package depends on `MSTest.TestFramework` 2.2.7, `MSTest.TestAdapter` 2.2.7, and `Microsoft.NET.Test.Sdk` 17.8.0.

## Playwright with xUnit v3

Code: `Microsoft.NET.Sdk` with `OutputType` `Exe`, `xunit.v3` 4.0.3, and `Microsoft.Playwright.Xunit.v3` 1.65.1. No `Microsoft.NET.Test.Sdk` and no `xunit.runner.visualstudio`.

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

public class OrderPageTests(ShopFixture shop, ITestOutputHelper output) : PageTest
{
    [Fact]
    public async Task Heading_is_visible()
    {
        var token = TestContext.Current.CancellationToken;
        output.WriteLine($"token can cancel: {token.CanBeCanceled}");
        await Page.SetContentAsync(shop.Html);
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Orders" })).ToBeVisibleAsync();
    }
}
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `Microsoft.Playwright.Xunit.v3` 1.65.1 works with `xunit.v3` 4.0.3 | pass | Build succeeded, 0 warnings. `total: 1, succeeded: 1`. |
| A `PageTest` class gets an assembly fixture and `ITestOutputHelper` in its primary constructor | pass | The test prints `token can cancel: True`. |

## WebApplicationFactory with Kestrel

Code: the web app has top-level statements and no `public partial class Program`. The test project references `Microsoft.AspNetCore.Mvc.Testing` and the web project.

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

[TestClass]
public class KestrelTests : PageTest
{
    private static WebApplicationFactory<Program> factory = null!;

    [ClassInitialize]
    public static void StartApp(TestContext context)
    {
        factory = new WebApplicationFactory<Program>();
        factory.UseKestrel(0);
        factory.StartServer();
    }

    [ClassCleanup]
    public static async Task StopApp() => await factory.DisposeAsync();

    [TestMethod]
    public async Task Home_page_shows_orders()
    {
        await Page.GotoAsync(factory.ClientOptions.BaseAddress.ToString());
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Orders" })).ToBeVisibleAsync();
    }
}
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `UseKestrel(0)` and `StartServer()` start a real server on a free port | pass | Log: `Now listening on: http://127.0.0.1:32959`. |
| `factory.ClientOptions.BaseAddress` gives the real address after `StartServer()` | pass | `http://127.0.0.1:32959/`, with a slash at the end. |
| `factory.CreateClient().BaseAddress` gives the real address | pass | `http://127.0.0.1:32959/`. `GetStringAsync("/api/ping")` returns `pong` over the real port. |
| `IServerAddressesFeature` gives the real address | pass | `factory.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses` gives `http://127.0.0.1:32959`, with no slash at the end. |
| Playwright opens the page on the real address | pass | The Chromium test passes. |
| `public partial class Program` is not necessary | pass | `WebApplicationFactory<Program>` compiles in the test project on `net10.0` and `net11.0` without the line. |

Use `factory.ClientOptions.BaseAddress`. It is the shortest form and it has the slash at the end, so a relative URL in `GotoAsync` works.

## Reqnroll with MSTest and Playwright

Code: `Sdk="MSTest.Sdk/4.5.3"` with `Reqnroll.MSTest` 3.3.4, `Microsoft.Playwright` 1.65.1, `Microsoft.AspNetCore.Mvc.Testing`, and the web project. No `reqnroll.json`. The browser starts one time in `[BeforeTestRun]`. Each scenario gets a new context and page. The `[AfterScenario]` hook saves the trace only for a failed scenario.

```gherkin
Feature: Orders

  Scenario: The home page shows the orders
    When the user opens the home page
    Then the user sees the heading "Orders"

  Scenario: A discount lowers the total
    Given an order of 100.00 euro
    When a discount of 10.50 euro is applied
    Then the total is 89.50 euro
```

```csharp
public sealed class BrowserSession
{
    public IBrowserContext Context { get; set; } = null!;
    public IPage Page { get; set; } = null!;
}

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

`OrderState` is a class with one property `decimal Total`. `Expect` comes from `using static Microsoft.Playwright.Assertions;`.

| Claim | Result | Evidence |
|-------|--------|----------|
| `Reqnroll.MSTest` 3.3.4 compiles on `MSTest.Sdk` 4.5.3 | pass | Build succeeded, 0 warnings. |
| Context injection of 2 POCO classes into hooks and steps | pass | Both scenarios pass. |
| `IBrowser` in `[BeforeTestRun]`, `IBrowserContext` and `IPage` in `[BeforeScenario]`, close in `[AfterScenario]` | pass | `total: 3, failed: 1, succeeded: 2`. The third scenario fails on purpose (heading "Missing"). |
| The hook saves the trace only for a failed scenario | pass | Only `traces/A failed scenario keeps its trace.zip` exists. The relative path is relative to `bin/Debug/<tfm>/`. |
| `{decimal}` in a step text | pass | `100.00 - 10.50` gives `89.50`, and the scenario passes. The same steps pass with `Reqnroll.xunit.v3`. |

## Reqnroll with xUnit v3

Code: `Microsoft.NET.Sdk` with `OutputType` `Exe`, `xunit.v3` 4.0.3, and `Reqnroll.xunit.v3` 3.3.4.

```csharp
using Reqnroll;
using Xunit;

[Binding]
public sealed class DiscountSteps(ITestOutputHelper output)
{
    private decimal total;

    [Given("an order of {decimal} euro")]
    public void GivenOrder(decimal amount) { total = amount; output.WriteLine($"order {amount}"); }
}
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `Reqnroll.xunit.v3` 3.3.4 with `xunit.v3` 4.0.3 compiles and runs | pass | Build succeeded, 0 warnings. `total: 1, succeeded: 1`. |
| A binding gets `Xunit.ITestOutputHelper` | pass | The output shows `order 100.00`. |

## xUnit v3 assembly attributes and global.json

```csharp
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]
[assembly: AssemblyFixture(typeof(ShopFixture))]

public sealed class ShopFixture : IAsyncLifetime
{
    public string Html { get; private set; } = "";
    public ValueTask InitializeAsync() { Html = "<h1>Orders</h1>"; return ValueTask.CompletedTask; }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

| Claim | Result | Evidence |
|-------|--------|----------|
| `[assembly: Parallelization(Mode = ParallelMode.None)]` compiles | pass, with 3 usings | `ParallelizationAttribute` is in `Xunit.v3`. `ParallelMode` is in `Xunit.Sdk`. |
| The attribute with only `using Xunit;` | fails | `error CS0246: The type or namespace name 'Parallelization' could not be found` and `error CS0103: The name 'ParallelMode' does not exist in the current context`. |
| `[assembly: AssemblyFixture(typeof(...))]` with `IAsyncLifetime` and `ValueTask` | pass | The fixture injects into the test class. The test passes. |
| `TestContext.Current.CancellationToken` | pass | `CanBeCanceled` is `True`. |
| `CollectionBehavior(DisableTestParallelization = true)` gives CS0619 | pass | `error CS0619: 'CollectionBehaviorAttribute.DisableTestParallelization' is obsolete: 'Please set ParallelizationAttribute.Mode instead. This property will be removed in the next major version.'` |
| On SDK 10, `dotnet test` with MTP needs the runner in `global.json` | pass | Without the `test` section: `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience.` Both the xUnit project and the MSTest project give this error. |
| On SDK 11, the same `global.json` setting is necessary | pass | Without the `test` section, SDK 11.0.100 gives the same error. `dotnet test --project` gives `MSB1001: Unknown switch`, because the VSTest mode does not know `--project`. |

This `global.json` works for MSTest and xUnit v3 on SDK 10.0.404. On SDK 11, change only the version to `11.0.100`:

```json
{
  "sdk": { "version": "10.0.404", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

## MSTest DataRow with decimal

| Form | Build | Run |
|------|-------|-----|
| `[DataRow(100.00, 10.50, 89.50)]` with `decimal` parameters | MSTEST0014 warning | fail: `Test data doesn't match method parameters. Either the count or types are different. Test expected 3 parameter(s), with types 'Decimal, Decimal, Decimal', but received 3 argument(s), with types 'Double, Double, Double'.` |
| `[DataRow("100.00", "10.50", "89.50")]` with `decimal` parameters | MSTEST0014 warning | fail: the same message with `String` |
| `[DataRow(100, 10, 90)]` with `decimal` parameters | no warning | fail: the same message with `Int32` |
| `[DataRow(100.00, 10.50, 89.50)]` with `double` parameters and a cast to `decimal` | no warning | pass |
| `[DynamicData]` with `IEnumerable<(decimal, decimal, decimal)>` | no warning | pass |
| `[DynamicData]` with `IEnumerable<TestDataRow<(decimal, decimal, decimal)>>` | no warning | pass |

The MSTEST0014 text:

```text
warning MSTEST0014: DataRow argument types do not match method parameter types. Parameter 'total' expects type 'decimal', but the provided value has type 'double'; ...
```

The working forms:

```csharp
public static IEnumerable<(decimal Total, decimal Discount, decimal Expected)> Discounts =>
[
    (100.00m, 10.50m, 89.50m),
    (20m, 0m, 20m),
];

[TestMethod]
[DynamicData(nameof(Discounts))]
public void Dynamic_data_tuples(decimal total, decimal discount, decimal expected) =>
    Assert.AreEqual(expected, Apply(total, discount));

public static IEnumerable<TestDataRow<(decimal, decimal, decimal)>> DiscountRows =>
[
    new((100.00m, 10.50m, 89.50m)) { DisplayName = "100 minus 10.50" },
];

[TestMethod]
[DynamicData(nameof(DiscountRows))]
public void Test_data_rows(decimal total, decimal discount, decimal expected) =>
    Assert.AreEqual(expected, Apply(total, discount));
```

## .NET 11

The same projects, with `net11.0`, `Microsoft.AspNetCore.Mvc.Testing` 11.0.0, and SDK 11.0.100. The Chromium of the `net10.0` install serves both runs.

| Claim | Result | Evidence |
|-------|--------|----------|
| Check 1: `PageTest` with `Microsoft.Playwright.MSTest.v4` on `net11.0` | pass | Build succeeded, 0 warnings. `total: 2, succeeded: 2`. |
| Check 3: `UseKestrel(0)` and `StartServer()` on ASP.NET Core 11 | pass | `ClientOptions.BaseAddress`, `CreateClient().BaseAddress`, and `IServerAddressesFeature` give the real address, as on .NET 10. Playwright opens the page. No `public partial class Program`. |
| Check 4: Reqnroll 3.3.4 with MSTest 4.5.3 and Playwright on `net11.0` | pass | Build succeeded, 0 warnings. `total: 3, failed: 1, succeeded: 2` (the failure is on purpose). Only the failed scenario has a trace. |
| `xunit.v3` 4.0.3 and `Microsoft.Playwright.Xunit.v3` on `net11.0` | pass | Build succeeded, 0 warnings. `total: 1, succeeded: 1`. |
| `Reqnroll.xunit.v3` 3.3.4 on `net11.0` | pass | Build succeeded, 0 warnings. `total: 1, succeeded: 1`. |

## Corrections to the research

1. **Plain `Microsoft.Playwright.MSTest` with MSTest 4 (playwright).** It does not fail at build. It builds with 7 warnings MSTEST0082 and then finds 0 tests (exit code 8). A skill must tell the agent that "build succeeded" does not prove the package is correct. The fix is `Microsoft.Playwright.MSTest.v4`.
2. **`Microsoft.Playwright.MSTest.v4` with MTP (playwright, not verified before).** It works with `MSTest.Sdk` 4.5.3 and no extra property. Mark it verified.
3. **`Microsoft.Playwright.Xunit.v3` with `xunit.v3` 4.0.3 (playwright, not verified before).** It works. Mark it verified.
4. **Kestrel address (playwright, not verified before).** After `StartServer()`, `factory.ClientOptions.BaseAddress` gives the real address with a slash at the end. `CreateClient()` and `IServerAddressesFeature` also give it. `public partial class Program` is not necessary on .NET 10 and .NET 11.
5. **Browser install (playwright).** `playwright.ps1` needs PowerShell. Without `pwsh`, the bundled driver works: `bin/Debug/<tfm>/.playwright/node/linux-x64/node bin/Debug/<tfm>/.playwright/package/cli.js install chromium`. The check did not test `Microsoft.Playwright.Program.Main(["install"])`.
6. **`ParallelizationAttribute` usings (xunit).** The research names the types correctly. A skill must name the usings: `using Xunit.v3;` and `using Xunit.Sdk;`. With only `using Xunit;`, the line gives CS0246 and CS0103.
7. **`global.json` runner (xunit, mstest).** The research says the MTP runner setting is necessary on SDK 10. It is also necessary on SDK 11.0.100. The error text is the same.
8. **MSTEST0014 (mstest).** An `int` literal such as `[DataRow(100, 10, 90)]` for `decimal` parameters gives no MSTEST0014 warning. Each row still fails at run time. A string literal gives MSTEST0014 and fails too. MSTest does no conversion to `decimal`. The rule must forbid each `DataRow` literal for a `decimal` parameter, not only `double` literals.
9. **`{decimal}` in Reqnroll (reqnroll, not verified before).** It works in Reqnroll 3.3.4 with MSTest and with xUnit v3.
10. **Reqnroll on .NET 11 (reqnroll, not verified before).** Reqnroll 3.3.4 with `Reqnroll.MSTest` and `Reqnroll.xunit.v3` builds and runs on `net11.0` with SDK 11.0.100. MSTest 4.5.3 and `xunit.v3` 4.0.3 also build and run there.
11. **`dotnet test` with MSBuild switches (all .NET test skills).** In MTP mode, `dotnet test --disable-build-servers` fails with `Unknown option` and exit code 5. Build first, then run `dotnet test --no-build`.

## Addendum: xUnit v3 examples of dotnet-testing

Date: 2026-11-21. The main session compiled and ran the 5 examples of `references/xunit-v3.md` that had no check, on SDK 10.0.404 with `xunit.v3` 4.0.3 and `Testcontainers.PostgreSql` 4.17.0. The domain types are small stubs.

| Claim | Result | Evidence |
|-------|--------|----------|
| Test class with `ITestOutputHelper` in the primary constructor and `TestContext.Current.CancellationToken` | pass | `dotnet test --no-build`: 4 passed, 2 skipped, 0 failed for the project |
| `DatabaseFixture` with `PostgreSqlBuilder("postgres:18.6-alpine")` as an assembly fixture | pass | Build succeeded. The test passed |
| `TheoryData<decimal, decimal>` with `[MemberData]` | pass | 2 rows passed |
| `Assert.SkipUnless` and `[Fact(Explicit = true)]` | pass | Both tests skipped on Linux |
| VSTest opt-out: `xunit.v3.mtp-off` 4.0.3, `xunit.runner.visualstudio` 4.0.1, `Microsoft.NET.Test.Sdk` 18.12.0, no `test` section in `global.json` | pass | `dotnet test`: 1 passed, in VSTest mode |
