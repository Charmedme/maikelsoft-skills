# MSTest 4

Facts for MSTest 4.5.1 with Microsoft.Testing.Platform (MTP) 2.5 on the .NET 10 SDK. Checked on 2026-10-16. Only the latest MSTest release gets support.

## Contents

- Project setup
- Runner and commands
- Analyzers
- Lifecycle
- Parallel runs and timeouts
- Data-driven tests
- MSTest 3 to 4: removed and changed APIs
- Behavior changes in version 4
- Traps

## Project setup

`global.json` at the repository root pins the SDK of all test projects and selects the runner:

```json
{
  "msbuild-sdks": {
    "MSTest.Sdk": "4.5.1"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

A unit test project (compiled: `examples/tests/Shop.UnitTests/Shop.UnitTests.csproj`):

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <MSTestAnalysisMode>Recommended</MSTestAnalysisMode>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="FluentAssertions" Version="[7.2.2,8.0.0)" />
    <PackageReference Include="NSubstitute" Version="6.2.0" />
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.11.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Shop/Shop.csproj" />
  </ItemGroup>
</Project>
```

- `MSTest.Sdk` adds the framework, the adapter, the analyzers, MTP, code coverage, and TRX reports. It no longer adds `Microsoft.NET.Test.Sdk`.
- A test project that references a web project and uses `WebApplicationFactory` works with `MSTest.Sdk` (compiled: `examples/tests/Shop.IntegrationTests`).
- A test project that needs the targets of `Microsoft.NET.Sdk.Web` itself: use `Microsoft.NET.Sdk`, the `MSTest` package, `<EnableMSTestRunner>true</EnableMSTestRunner>`, and `<OutputType>Exe</OutputType>`. `MSTest.Sdk` derives only from `Microsoft.NET.Sdk`.
- `<UseVSTest>true</UseVSTest>` switches back to VSTest. Use it only when a tool in the pipeline cannot run MTP.
- With Central Package Management, put `MSTest` in `Directory.Packages.props` only when you use the package instead of the SDK.
- An Aspire AppHost test project sets `<EnableAspireTesting>true</EnableAspireTesting>`. The `aspire` skill covers the rest.

## Runner and commands

With MTP, each test project builds to an executable that runs its own tests.

| Task | Command |
|------|---------|
| Run all test projects | `dotnet test` |
| Run one project | `dotnet test --project tests/Shop.UnitTests/Shop.UnitTests.csproj` |
| Filter | `dotnet test --filter "FullyQualifiedName~DiscountTests"` |
| TRX report | `dotnet test --report-trx` |
| Coverage | `dotnet test --coverage --coverage-output-format cobertura` |
| Run the executable | `dotnet bin/Debug/net10.0/Shop.UnitTests.dll` |

- VSTest options such as `--logger trx` or `--collect "XPlat Code Coverage"` do not work with MTP.
- MTP v2 on the .NET 10 SDK refuses the VSTest path of `dotnet test`. Without the `global.json` runner setting, `dotnet test` fails.

## Analyzers

`<MSTestAnalysisMode>Recommended</MSTestAnalysisMode>` makes the Info rules warnings. Build with warnings as errors in CI. The rules that matter most:

| Rule | Finds |
|------|-------|
| MSTEST0001 | No `[assembly: Parallelize]` or `[assembly: DoNotParallelize]` |
| MSTEST0017 | `Assert.AreEqual(actual, expected)`: the argument order is wrong |
| MSTEST0024 | `TestContext` in a static field |
| MSTEST0040 | Assert in an `async void` context |
| MSTEST0044 | `[DataTestMethod]`: use `[TestMethod]` |
| MSTEST0045 | `[Timeout]` without `CooperativeCancellation = true` |
| MSTEST0049 | `TestContext.CancellationToken` does not flow to an async call |
| MSTEST0052 | Explicit `DynamicDataSourceType`: let MSTest detect it |
| MSTEST0065 | `Assert.AreEqual` on a collection compares references |
| MSTEST0067 | `Thread.Sleep`, `.Wait()`, `.Result` in tests (off in Recommended, turn it on with `dotnet_diagnostic.MSTEST0067.severity = warning`) |
| MSTEST0074 to MSTEST0077 | Parallel tests that change culture, the current folder, or environment variables |
| MSTEST0082 | A base class from another MSTest major version: MSTest loses its tests |

## Lifecycle

The order for each test: constructor, `TestContext` property, `[GlobalTestInitialize]`, `[TestInitialize]`, test, `[TestCleanup]`, `[GlobalTestCleanup]`, `DisposeAsync`, `Dispose`.

- An assembly has at most one `[AssemblyInitialize]` and one `[AssemblyCleanup]`. Put them in one `static` class.
- `[ClassCleanup]` always runs at the end of its class in version 4.
- `TestContext.TestName` throws in `[ClassInitialize]` and `[AssemblyInitialize]`.
- Use the constructor for sync setup. Use `[TestInitialize]` for async setup.

## Parallel runs and timeouts

```csharp
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
```

- Since 4.3 you can also set `<MSTestParallelizeScope>MethodLevel</MSTestParallelizeScope>` in MSBuild.
- Mark a class that uses a shared resource (one database) with `[DoNotParallelize]`.
- `[ResourceLock("name")]` (4.4) serializes only the tests that share a named resource.
- `[Timeout(5000, CooperativeCancellation = true)]` cancels `TestContext.CancellationToken`. Without `CooperativeCancellation`, the test keeps running in the background.
- AppDomain isolation is off under MTP. A foreground thread that a test leaks makes the run hang.

## Data-driven tests

Compiled in `examples/tests/Shop.UnitTests/DiscountTests.cs` and `MstestApiSnippets.cs`:

```csharp
[TestMethod]
[DataRow(99.99, 0.0, DisplayName = "Below first tier")]
[DataRow(100.0, 5.0, DisplayName = "First tier boundary")]
public void For_TotalAtTier_ReturnsTierDiscount(double total, double expected)
{
    decimal discount = Discount.For((decimal)total);

    discount.Should().Be((decimal)expected);
}

[TestMethod]
[DynamicData(nameof(Rows))]
public void For_Row_ReturnsExpected(decimal total, decimal expected)
{
    decimal discount = Discount.For(total);

    Assert.AreEqual(expected, discount);
}

public static IEnumerable<TestDataRow<(decimal Total, decimal Expected)>> Rows =>
[
    new((50m, 0m)) { DisplayName = "No discount below 100" },
    new((100m, 5m)) { DisplayName = "Five percent at 100" },
];
```

- An attribute cannot hold a `decimal`. Use `double` in `[DataRow]` and cast, or use `[DynamicData]`.
- `[DynamicData]` reads a property or a method. Do not pass `DynamicDataSourceType` (MSTEST0052).
- Return tuples or `TestDataRow<T>`, not `IEnumerable<object[]>`. The compiler checks the types.

## MSTest 3 to 4: removed and changed APIs

| MSTest 3 | MSTest 4 |
|----------|----------|
| `[ExpectedException(typeof(X))]` | `Assert.ThrowsExactly<X>(() => ...)` |
| `Assert.ThrowsException<X>`, `ThrowsExceptionAsync<X>` | `Assert.Throws<X>`, `ThrowsExactly<X>`, `ThrowsAsync<X>`, `ThrowsExactlyAsync<X>` |
| `Assert.IsInstanceOfType<T>(x, out var t)` | `var t = Assert.IsInstanceOfType<T>(x);` |
| `Assert.AreEqual(a, b, "msg {0}", arg)` | `Assert.AreEqual(a, b, $"msg {arg}")` |
| `[ClassCleanup(ClassCleanupBehavior.EndOfClass)]` | `[ClassCleanup]` |
| `[Timeout(TestTimeout.Infinite)]` | `[Timeout(int.MaxValue)]` |
| `TestContext.Properties.Contains(k)` | `TestContext.Properties.ContainsKey(k)` |
| `TestContext.ManagedType` | `TestContext.FullyQualifiedTestClassName` |
| `[TestMethod("name")]` | `[TestMethod(DisplayName = "name")]` |
| `[DataTestMethod]` | `[TestMethod]` |
| Custom `TestMethodAttribute.Execute` | `ExecuteAsync` |
| `ConditionBaseAttribute.ShouldRun` | `IsConditionMet` |
| .NET Core 3.1 to .NET 7 | .NET 8 or later, or .NET Framework 4.6.2 or later |

New asserts in version 4: `Assert.AreSequenceEqual` (4.3) for collections, `Assert.That(() => expression)` for a readable message. `Assert.Scope()` is experimental (`MSTESTEXP`): a value-returning assert returns `null` on failure inside the scope.

For a full migration from MSTest 3, or from VSTest to MTP, use the `dotnet-test-migration` plugin of github.com/dotnet/skills when the environment has it.

## Behavior changes in version 4

- `TreatDiscoveryWarningsAsErrors` is on.
- `TestCase.Id` changed. Test history in Azure DevOps starts again one time.
- Version 4 is not binary compatible with libraries built against version 3. Rebuild shared test libraries.
- The assembly name changed from `Microsoft.VisualStudio.TestPlatform.TestFramework` to `MSTest.TestFramework`. This is why FluentAssertions 7 does not find MSTest 4.

## Traps

- `Assert.AreEqual` on two lists compares references (MSTEST0065). Use `Assert.AreSequenceEqual` or `Should().Equal(...)`.
- MSTest is `expected, actual`. A swapped order gives wrong labels in the failure message (MSTEST0017).
- A static `TestContext` gives wrong output in parallel runs (MSTEST0024).
- Only one `[AssemblyInitialize]`. A second one is an error at discovery.
