# Project setup

Packages, project files, `global.json`, `reqnroll.json`, the run, and the move from SpecFlow. The rule numbers refer to `SKILL.md`. A compile check built and ran these files on SDK 10.0.404 (`net10.0`) and SDK 11.0.100 (`net11.0`).

## Contents

- Packages and versions
- MSTest 4 project
- xUnit v3 project
- global.json
- reqnroll.json
- Build and run
- Tags and filters
- HTML report
- Known issues
- Move from SpecFlow

## Packages and versions

| Package | Version | Note |
|---------|---------|------|
| `Reqnroll.MsTest` | 3.3.4 | MSTest 4 needs Reqnroll 3.2 or later. Reqnroll 2.x does not support MSTest 4 |
| `Reqnroll.xunit.v3` | 3.3.4 | For `xunit.v3`. `Reqnroll.xUnit` is for xUnit v2 |
| `Reqnroll.NUnit`, `Reqnroll.TUnit` | 3.3.4 | Only where the repository uses NUnit or TUnit |
| `Reqnroll.Microsoft.Extensions.DependencyInjection` | 3.3.4 | Needs Reqnroll `[3.3.4, 4.0.0)`. Only with RQ-05 |
| `Reqnroll.SpecFlowCompatibility` | 3.3.3 | Only for a fast first step of a move from SpecFlow |
| `MSTest.Sdk` | 4.5.3 | The project SDK of an MSTest 4 project |
| `xunit.v3` | 4.0.3 | Runs on Microsoft.Testing.Platform (MTP) v2 by default |
| `Microsoft.Playwright` | 1.65.1 | Only for browser steps. No adapter package (RQ-02) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.15 (`net10.0`), 11.0.0 (`net11.0`) | For `UseKestrel(0)` and `StartServer()` |

- Pin each version in `Directory.Packages.props`. No `Version="*"`.
- Keep each Reqnroll package on the same version.
- The Reqnroll compatibility page stops at .NET 10.
- The compile check also ran Reqnroll 3.3.4 on `net11.0`, with MSTest 4.5.3 and with `xunit.v3` 4.0.3.

## MSTest 4 project

Pin the version of `MSTest.Sdk` in `global.json` (`msbuild-sdks`), as `test-stack:dotnet-testing` does. The project file then has `Sdk="MSTest.Sdk"` with no version. The compile check used the other form, `Sdk="MSTest.Sdk/4.5.3"`. It also works, but keep one form in the repository. Add `Microsoft.Playwright` and the web project only for browser steps.

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Shop.Acceptance</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Reqnroll.MsTest" />
    <PackageReference Include="Microsoft.Playwright" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Shop.Web\Shop.Web.csproj" />
  </ItemGroup>
</Project>
```

- No `MSTest.TestAdapter`, `MSTest.TestFramework`, or `Microsoft.NET.Test.Sdk` reference. `MSTest.Sdk` brings them. The Reqnroll MSTest docs still name these packages and VSTest.
- With MSTest packages instead of `MSTest.Sdk`, keep `MSTest.TestFramework` and `MSTest.TestAdapter` on the same version (Reqnroll #1126).
- `<RootNamespace>` prevents the CS0246 error on `[Binding]` when the project name contains the name of a test framework (rule 19).
- Inject `TestContext` in a binding constructor. A static `[BeforeTestRun]` or `[AfterTestRun]` gets `TestContext` as a parameter.
- For the `Parallelize` attribute and the other MSTest rules, read `test-stack:dotnet-testing`.

## xUnit v3 project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Reqnroll.xunit.v3" />
  </ItemGroup>
</Project>
```

- No `xunit.runner.visualstudio` and no `Microsoft.NET.Test.Sdk`. They are for VSTest.
- A binding gets `Xunit.ITestOutputHelper` through its constructor (`using Xunit;`). `Xunit.Abstractions` is the xUnit v2 namespace.

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

## global.json

On SDK 10 and SDK 11, `dotnet test` with MTP needs the `test` section. Without it, both SDKs stop with "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later". On SDK 11, change only the version to `11.0.100`.

```json
{
  "sdk": { "version": "10.0.404", "rollForward": "latestFeature" },
  "msbuild-sdks": { "MSTest.Sdk": "4.5.3" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

One `global.json` serves each test project of the repository. MSTest and xUnit projects in one repository use the same runner mode.

## reqnroll.json

A project runs without `reqnroll.json`. Add it for a setting, with the schema:

```json
{
  "$schema": "https://schemas.reqnroll.net/reqnroll-config-latest.json",
  "language": { "feature": "en-US" },
  "formatters": {
    "html": { "outputFilePath": "reqnroll_report.html" }
  }
}
```

- `language/feature`: the default language of a feature file without a `# language:` header (RQ-04).
- `generator/addNonParallelizableMarkerForTags`: a list of tags. Features with these tags do not run in parallel.
- `generator/allowRowTests`: `false` makes one test method for each `Examples` row. This is the workaround for tags on `Examples` with MSTest (section "Tags and filters").
- Reqnroll also reads `specflow.json`. Rename it to `reqnroll.json` and add the schema.

## Build and run

```sh
dotnet build
dotnet test --no-build
```

- In MTP mode, `dotnet test` gives MSBuild switches to the test app. `dotnet test --disable-build-servers` stops with "Unknown option" and exit code 5. Build first, then test with `--no-build` (rule 17).
- `REQNROLL_DRY_RUN=true` runs the scenarios without the binding code. Use it to find undefined steps.
- Reqnroll generates async test code.

## Tags and filters

- MSTest makes a `TestCategory` from each feature tag and scenario tag:

  ```sh
  dotnet test --no-build --filter "TestCategory=web"
  ```

- MSTest does not give the tags of an `Examples` block to the runner. Put the tag on the `Scenario Outline` (rule 18). When each `Examples` block needs its own tag, set `"generator": { "allowRowTests": false }`.

## HTML report

The HTML formatter writes one HTML page with each feature, scenario, and step result (RQ-07). It replaces SpecFlow+ LivingDoc, which has no Reqnroll version.

- In `reqnroll.json`: `"formatters": { "html": { "outputFilePath": "reqnroll_report.html" } }`.
- Or for one run, with an environment variable:

  ```sh
  REQNROLL_FORMATTERS_HTML='outputFilePath=reqnroll_report.html' dotnet test --no-build
  ```

- The `message` formatter writes Cucumber Messages (`.ndjson`) for other tools.
- Since 3.3.0, the output path takes variables for build data and time.

## Known issues

| Issue | Effect | What to do |
|-------|--------|------------|
| #1117 | A throw in an `[AfterScenario]` hook skips the later `[AfterScenario]` hooks | No throw in cleanup hooks. `finally` for each close |
| #1106 | A project name with the name of a test framework drops `Reqnroll.dll` (CS0246) | Set `<RootNamespace>` |
| #1126 | .NET Framework 4.8.1 with MSTest 4.5.1 finds no tests | Keep the MSTest packages on one version. Do not move the suite to another .NET version to fix it |
| #1083 | A scenario can go missing from the HTML report after a cleanup error | No throw in cleanup hooks |

## Move from SpecFlow

SpecFlow reached its end of life on 2024-12-31. The steps:

1. Replace the packages, the namespaces, and the configuration file, as in the table.
2. Optional: to move in two steps, reference `Reqnroll.SpecFlowCompatibility` first. It keeps the `TechTalk.SpecFlow` names.
3. Replace `ScenarioContext.Current` and the string keys with injected POCOs (rule 8).
4. Build, then run the tests. Done when the same number of tests passes as before the move.

| SpecFlow (old) | Reqnroll (new) |
|----------------|----------------|
| `SpecFlow.MsTest`, `SpecFlow.xUnit`, `SpecFlow.NUnit` | `Reqnroll.MsTest`, `Reqnroll.xunit.v3` (or `Reqnroll.xUnit` on xUnit v2), `Reqnroll.NUnit` |
| `CucumberExpressions.SpecFlow.*` | Remove it. Reqnroll has Cucumber expressions built in |
| `using TechTalk.SpecFlow;` | `using Reqnroll;` |
| `using TechTalk.SpecFlow.Assist;` | `using Reqnroll;` (the table helpers are in this namespace) |
| `BoDi` | `Reqnroll.BoDi` |
| `ISpecFlowOutputHelper` | `IReqnrollOutputHelper` |
| `Table` | `DataTable` (`CreateInstance<T>`, `CreateSet<T>`, `CompareToInstance<T>`, `CompareToSet<T>`) |
| `[Given(Regex = "...")]` | `[Given(Expression = "...")]` |
| `specflow.json` | `reqnroll.json` with the schema |
| SpecFlow+ LivingDoc | The HTML formatter |
| Sync generated test code | Async generated test code |
