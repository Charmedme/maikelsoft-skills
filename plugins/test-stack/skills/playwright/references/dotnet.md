# Playwright for .NET

`Microsoft.Playwright` 1.65.1 with MSTest 4 or xUnit v3, on .NET 10 or .NET 11. The rule numbers refer to `SKILL.md`. The test project rules that are not about the browser (`MSTest.Sdk`, `global.json`, analyzers, names) are in `test-stack:dotnet-testing`.

## Contents

- Packages
- MSTest 4 project
- xUnit v3 project
- Start the app under test
- A test with PageTest
- Browser install
- Settings on Microsoft.Testing.Platform
- Parallel runs and retries
- CI
- Aspire

## Packages

| Test framework | Adapter | Note |
|----------------|---------|------|
| MSTest 4 (`MSTest.Sdk` 4.x) | `Microsoft.Playwright.MSTest.v4` | Namespace `Microsoft.Playwright.MSTest`. Depends on `MSTest.TestFramework` 4.x only |
| MSTest 2 or 3 | `Microsoft.Playwright.MSTest` | With MSTest 4, the build passes but the run finds 0 tests |
| xUnit v3 (`xunit.v3` 4.x) | `Microsoft.Playwright.Xunit.v3` | Template `dotnet new xunit3` |
| xUnit v2 | `Microsoft.Playwright.Xunit` | xUnit v2 only |
| NUnit | `Microsoft.Playwright.NUnit` | NUnit 3.13.2 or later |

- Each adapter is at 1.65.1 and brings `Microsoft.Playwright` 1.65.1. Pin the version in `Directory.Packages.props`.
- The packages target `netstandard2.0`. They have no `net10.0` or `net11.0` asset.
- The Playwright docs name only `Microsoft.Playwright.MSTest`. With MSTest 4, the old package builds with 7 warnings MSTEST0082: MSTest does not run the inherited `[TestInitialize]` of `PageTest`. Then the run finds 0 tests and exits with code 8. A green build does not prove the package. The fix is the `.v4` package. The test code does not change.
- `Microsoft.Playwright.MSTest.v4` 1.65.1 works with `MSTest.Sdk` 4.5.3 on MTP with no extra property. `Microsoft.Playwright.Xunit.v3` 1.65.1 works with `xunit.v3` 4.0.3. Both run on `net10.0` and `net11.0` (compile check of 2026-11-21).

## MSTest 4 project

`Directory.Packages.props` pins the versions:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Microsoft.Playwright.MSTest.v4" Version="1.65.1" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.15" />
  </ItemGroup>
</Project>
```

The test project uses `MSTest.Sdk`. The version of the SDK is in `global.json` (`test-stack:dotnet-testing`, rule 1).

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright.MSTest.v4" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Shop.Web\Shop.Web.csproj" />
  </ItemGroup>
</Project>
```

## xUnit v3 project

`xunit.v3` 4.x runs on Microsoft.Testing.Platform v2 (`xunit.v3` brings `xunit.v3.mtp-v2`). Do not add `xunit.runner.visualstudio` or `Microsoft.NET.Test.Sdk`: they are for VSTest.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Microsoft.Playwright.Xunit.v3" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Shop.Web\Shop.Web.csproj" />
  </ItemGroup>
</Project>
```

Pin `xunit.v3` to 4.0.3 and `Microsoft.Playwright.Xunit.v3` to 1.65.1 in `Directory.Packages.props`.

## Start the app under test

A browser cannot reach the in-memory `TestServer` of `WebApplicationFactory`. Since .NET 10, the factory can start a real Kestrel server (PW-03):

- `UseKestrel(0)` selects Kestrel on a free port. Call it before `CreateClient()` or `StartServer()`.
- `StartServer()` starts the server.
- On .NET 10 and later, a source generator makes `public partial class Program`. Remove an explicit declaration from the app.
- After `StartServer()`, `factory.ClientOptions.BaseAddress` is the real address, with a slash at the end (`http://127.0.0.1:<port>/`). A relative URL in `GotoAsync` works with it.
- Do not override `CreateHost` to build a second host with `UseUrls`, and do not look for a free port yourself.

Start the server once for the test assembly. The app keeps one database for all tests, so each test makes its own data (rule 4). For the database container and the reset, read `test-stack:dotnet-testing`.

MSTest 4:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace Shop.E2E;

[TestClass]
public static class AppServer
{
    private static WebApplicationFactory<Program>? factory;

    public static string BaseUrl { get; private set; } = "";

    [AssemblyInitialize]
    public static void Start(TestContext context)
    {
        factory = new WebApplicationFactory<Program>();
        factory.UseKestrel(0);
        factory.StartServer();
        BaseUrl = factory.ClientOptions.BaseAddress.ToString();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (factory is not null)
        {
            await factory.DisposeAsync();
        }
    }
}
```

xUnit v3, with an assembly fixture:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

[assembly: AssemblyFixture(typeof(Shop.E2E.AppServer))]

namespace Shop.E2E;

public sealed class AppServer : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> factory = new();

    public string BaseUrl { get; private set; } = "";

    public ValueTask InitializeAsync()
    {
        factory.UseKestrel(0);
        factory.StartServer();
        BaseUrl = factory.ClientOptions.BaseAddress.ToString();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => factory.DisposeAsync();
}
```

## A test with PageTest

The base class gives each test a new browser context and page, and `Expect(...)`. Override `ContextOptions()` for the base URL, the viewport, and the locale.

MSTest 4:

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace Shop.E2E;

[TestClass]
public sealed class OrderListTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = AppServer.BaseUrl,
        Locale = "en-US",
    };

    [TestMethod]
    public async Task OrderList_PaidOrder_ShowsStatusPaid()
    {
        var orderNumber = await TestData.CreatePaidOrderAsync(AppServer.BaseUrl, TestContext.CancellationToken);

        await Page.GotoAsync("/orders");

        var row = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = orderNumber });
        await Expect(row).ToBeVisibleAsync();
        await Expect(row.GetByRole(AriaRole.Cell, new() { Name = "Paid" })).ToBeVisibleAsync();
    }
}
```

xUnit v3:

```csharp
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;

namespace Shop.E2E;

public sealed class OrderListTests(AppServer app) : PageTest
{
    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = app.BaseUrl };

    [Fact]
    public async Task OrderList_PaidOrder_ShowsStatusPaid()
    {
        var orderNumber = await TestData.CreatePaidOrderAsync(app.BaseUrl, TestContext.Current.CancellationToken);

        await Page.GotoAsync("/orders");

        var row = Page.GetByRole(AriaRole.Row).Filter(new() { HasText = orderNumber });
        await Expect(row).ToBeVisibleAsync();
    }
}
```

- `TestData.CreatePaidOrderAsync` is a helper of the project. It makes the order through the API and returns a unique order number.
- Without a base class, `using static Microsoft.Playwright.Assertions;` gives `Expect(...)`.
- A failed `Expect` throws `PlaywrightAssertionException` (1.65).
- In 1.65, `NormalizeAsync()` on a locator prints it as C# code.

## Browser install

Install the browsers from the build output, after the build. Use the target framework of the project in the path:

```sh
dotnet build tests/Shop.E2E
pwsh tests/Shop.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
```

Without PowerShell, use the Node.js driver in the build output:

```sh
cd tests/Shop.E2E
bin/Debug/net10.0/.playwright/node/linux-x64/node bin/Debug/net10.0/.playwright/package/cli.js install chromium
```

- `--with-deps` adds the Linux libraries.
- `--no-remove` keeps the browsers of other Playwright installs (1.63).
- On Linux, the Node.js 26 in the 1.65 package needs `libatomic1`. Make sure that the CI image has it.
- Microsoft deprecated the dotnet tool `Microsoft.Playwright.CLI` (last version 1.2.3, 2022). Do not install it.
- Browsers in 1.65.1: Chromium 159, Firefox 159, WebKit 27.2.

## Settings on Microsoft.Testing.Platform

`MSTest.Sdk` and xUnit v3 run on Microsoft.Testing.Platform (MTP). The `<Playwright>` section of `.runsettings` (`BrowserName`, `LaunchOptions`, `ExpectTimeout`, `ActionTimeout`) goes through a VSTest settings provider. MTP does not call it. Playwright ignores the section, and nothing reports it.

| Setting | On MTP |
|---------|--------|
| Browser | Environment variable `BROWSER=chromium` (or `firefox`, `webkit`) |
| Visible browser | `HEADED=1` |
| Playwright Inspector | `PWDEBUG=1` |
| Base URL, viewport, locale, color scheme | `ContextOptions()` in the test class |
| Launch options, assertion timeouts | No MTP setting. Keep the defaults, or ask the human |

- On MTP 2.3 or later, the `environmentVariables` section of `testconfig.json` can set `BROWSER`, `HEADED`, and `PWDEBUG`. A maintainer comment in playwright-dotnet issue 3375 says so. It cannot set launch options, context options, or assertion timeouts.
- `dotnet test -- Playwright.BrowserName=chromium` in the docs is the VSTest form. It does nothing on MTP.
- A package `Microsoft.Playwright.Testing.Platform` that reads a `playwright` section from `testconfig.json` is an open pull request. It is not on NuGet. Do not reference it.
- Visual Studio Test Explorer can also ignore `LaunchOptions` from `.runsettings`.

## Parallel runs and retries

- MSTest: Playwright runs test classes in parallel and the tests of one class one after the other (`ExecutionScope.ClassLevel`). Set `[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]` in the end-to-end project.
- Playwright for .NET does not retry. Use `[Retry]` (MSTest) or the MTP retry extension (MSTest, xUnit v3). The major version of the retry package must match the MTP version: xUnit v3 4.x needs 2.x. Each attempt gets a new context and page.
- Retries hide flaky tests. Use them only after decision PW-06, and report each test that passed on a retry.

## CI

A GitHub Actions job:

```yaml
jobs:
  e2e:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v5
      - uses: actions/setup-dotnet@v5
        with:
          global-json-file: global.json
      - run: dotnet build tests/Shop.E2E
      - run: pwsh tests/Shop.E2E/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium
      - run: dotnet test --project tests/Shop.E2E --no-build
        env:
          BROWSER: chromium
```

- The test run uses the build of the step before (`--no-build`), so `playwright.ps1` and the tests use the same output folder.
- On MTP, `dotnet test` gives MSBuild switches such as `--disable-build-servers` to the test app, which stops with "Unknown option". Build first, then run `dotnet test --no-build`.

## Aspire

When the repository has an Aspire AppHost, start it with `Aspire.Hosting.Testing` and give its web endpoint to `ContextOptions()` as the base URL (PW-03). The AppHost side and the call that reads the endpoint are in `dev-stack:aspire`.

A deployed URL (`BASE_URL` from the environment) is only for a smoke test after a deploy. It cannot reset data, so the test must not change data that a user sees.
