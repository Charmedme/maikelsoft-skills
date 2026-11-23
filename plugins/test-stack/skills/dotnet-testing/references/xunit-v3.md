# xUnit v3

Facts for `xunit.v3` 4.0.3 with Microsoft.Testing.Platform (MTP) v2 on the .NET 10 SDK. Checked on 2026-11-21. A build on SDK 10.0.404 and SDK 11.0.100 checked the setup, the assembly attributes, the assembly fixture, and the cancellation token. Use this file only in a repository that already uses xUnit (TEST-01). The rules for test design, names, assertions, fakes, and containers in `SKILL.md` apply to xUnit too.

## Contents

- Project setup
- Test class shape
- Lifetime and fixtures
- Containers
- Theory data
- Skip and explicit tests
- Parallel runs
- Assertion libraries
- Move from xUnit v2
- Traps

## Project setup

`xunit.v3` 4.x depends on `xunit.v3.mtp-v2`, so MTP v2 is the default runner. Version 4.0 removed the support for MTP v1 and Mono. Each xUnit v3 test project is an executable. Targets: `net8.0` or later, or `net472`.

`global.json` at the repository root selects the runner for `dotnet test`. The .NET 10 and .NET 11 SDKs refuse the VSTest path for an MTP project. The error starts with "Testing with VSTest target is no longer supported". This file works (build-checked):

```json
{
  "sdk": { "version": "10.0.404", "rollForward": "latestFeature" },
  "test": { "runner": "Microsoft.Testing.Platform" }
}
```

One `global.json` serves each test project. MSTest projects and xUnit projects in one repository use the same runner mode (TEST-19). When the repository also has MSTest projects, the same file pins `MSTest.Sdk` under `msbuild-sdks`.

A test project (build-checked):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="xunit.v3" Version="4.0.3" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../src/Shop/Shop.csproj" />
  </ItemGroup>
</Project>
```

- An MTP project does not need `Microsoft.NET.Test.Sdk` or `xunit.runner.visualstudio`. These packages are for the VSTest mode only.
- `xunit.v3` brings the analyzers (xunit.analyzers 2.3.0 with 4.0.3). Build with warnings as errors in CI.
- Build first, then run `dotnet test --no-build`. In MTP mode, `dotnet test` sends MSBuild switches such as `--disable-build-servers` to the test app, which stops with "Unknown option" (exit code 5).

VSTest opt-out: only when a tool in the pipeline cannot run MTP, and then for each test project of the repository. Replace `xunit.v3` with `xunit.v3.mtp-off`, add the VSTest packages, and remove the `test` section from `global.json`:

```xml
<ItemGroup>
  <PackageReference Include="xunit.v3.mtp-off" Version="4.0.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="4.0.1" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.12.0" />
</ItemGroup>
```

## Test class shape

- A test is a `[Fact]` method, or a `[Theory]` method with data. The class needs no attribute.
- `ITestOutputHelper` is in namespace `Xunit`. Get it in the constructor.
- An `async void` test fails. Return `Task` or `ValueTask`.
- `TestContext.Current` gives the test, the output, the `CancellationToken`, and the fixtures (`GetFixture<T>()`). Read it in the test. Do not store it in a field.
- Pass `TestContext.Current.CancellationToken` to each async call. The analyzer xUnit1051 finds a missing token.

This test class gets the output helper and passes the token:

```csharp
using Xunit;

namespace Shop.UnitTests;

public sealed class OrderServiceTests(ITestOutputHelper output)
{
    [Fact]
    public async Task CloseAsync_OpenOrder_ClosesOrder()
    {
        var store = new FakeOrderStore();
        var sut = new OrderService(store);

        await sut.CloseAsync(7, TestContext.Current.CancellationToken);

        output.WriteLine($"Orders in store: {store.Count}");
        Assert.True(store.Get(7).IsClosed);
    }
}
```

`[assembly: CaptureConsole]` and `[assembly: CaptureTrace]` send `Console` and `Trace` output to the test output.

## Lifetime and fixtures

- Sync setup: the constructor. Sync cleanup: `IDisposable`.
- Async setup and cleanup: `IAsyncLifetime`. Its methods return `ValueTask`, and it inherits from `IAsyncDisposable`. When `InitializeAsync` throws, xUnit does not call `DisposeAsync`. Release each resource that `InitializeAsync` made before the throw.

| Scope | Form |
|-------|------|
| One test class | The class implements `IClassFixture<T>`. xUnit gives `T` to the constructor. |
| A group of classes | `ICollectionFixture<T>` on a `[CollectionDefinition]` class, and `[Collection]` on each test class |
| The whole assembly | `[assembly: AssemblyFixture(typeof(T))]`. xUnit gives `T` to the constructor of each test class. |

An assembly fixture with `IAsyncLifetime` (build-checked):

```csharp
using Xunit;

[assembly: AssemblyFixture(typeof(Shop.IntegrationTests.ShopFixture))]

namespace Shop.IntegrationTests;

public sealed class ShopFixture : IAsyncLifetime
{
    public string Html { get; private set; } = "";

    public ValueTask InitializeAsync()
    {
        Html = "<h1>Orders</h1>";
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
```

## Containers

Start one container for the test assembly in an assembly fixture. Do not use `ICollectionFixture` for this: an assembly fixture needs no `[Collection]` on each class. The rules for images, ports, Respawn, and `WebApplicationFactory` in `SKILL.md` and `references/testcontainers.md` apply.

```csharp
using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(Shop.IntegrationTests.DatabaseFixture))]

namespace Shop.IntegrationTests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
        => await _container.StartAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

public sealed class OrderStoreTests(DatabaseFixture database)
{
    [Fact]
    public async Task SaveAsync_NewOrder_CanReadItBack()
    {
        var store = new OrderStore(database.ConnectionString);

        await store.SaveAsync(new Order(7, 10m), TestContext.Current.CancellationToken);

        var order = await store.GetAsync(7, TestContext.Current.CancellationToken);
        Assert.Equal(10m, order.Total);
    }
}
```

`Testcontainers.XunitV3` 4.17.0 needs `xunit.v3.extensibility.core` 3.2.2 or later. It gives base classes for the same job. `ContainerFixture<TBuilder, TContainer>` for a shared container, `ContainerTest<TBuilder, TContainer>` for one container for each test, and `DbContainerFixture` and `DbContainerTest` for ADO.NET. Use them when the project already uses them. Share a `ContainerFixture` with `[assembly: AssemblyFixture(...)]`.

Database tests share one database. Run the integration test assembly without parallel runs (see "Parallel runs"), and reset the data with Respawn in `InitializeAsync` of the test class.

## Theory data

- `[InlineData]` for simple values that an attribute can hold.
- `TheoryData<T1, T2>` with `[MemberData]` for `decimal`, dates, and objects. The compiler checks the types.
- `TheoryDataRow<T1, T2>` adds `Skip`, `Label`, and a display name to one row. `MatrixTheoryData` makes each combination of two sets. `[MemberData]` can read async data.

```csharp
public static TheoryData<decimal, decimal> TierRows => new()
{
    { 50m, 0m },
    { 100m, 5m },
};

[Theory]
[MemberData(nameof(TierRows))]
public void For_TotalAtTier_ReturnsTierDiscount(decimal total, decimal expected)
{
    decimal discount = Discount.For(total);

    Assert.Equal(expected, discount);
}
```

## Skip and explicit tests

- Use `Skip = "reason"` only for a test that must not run now (with an issue link).
- For a condition, skip in the test: `Assert.Skip("reason")`, `Assert.SkipUnless(condition, "reason")`, `Assert.SkipWhen(condition, "reason")`. Or use `[Fact(SkipUnless = nameof(Property))]` with a static `bool` property.
- `[Fact(Explicit = true)]` runs only when you ask for this test. Use it for a slow or manual test.

```csharp
[Fact]
public void ExportPath_OnWindows_UsesBackslash()
{
    Assert.SkipUnless(OperatingSystem.IsWindows(), "The test needs Windows paths.");

    string path = ExportPath.For("orders");

    Assert.Contains('\\', path);
}

[Fact(Explicit = true)]
public async Task Import_FullFile_Completes()
{
    await Importer.RunAsync("data/full.csv", TestContext.Current.CancellationToken);
}
```

## Parallel runs

xUnit runs test collections in parallel. By default, each test class is one collection. Version 4.0 sets the parallel options with `ParallelizationAttribute` (namespace `Xunit.v3`) and `ParallelMode` (namespace `Xunit.Sdk`). With only `using Xunit;`, the line gives CS0246 and CS0103.

To run an integration test assembly without parallel runs (build-checked):

```csharp
using Xunit.Sdk;
using Xunit.v3;

[assembly: Parallelization(Mode = ParallelMode.None)]
```

- `MaxThreads` and `Algorithm` on the same attribute set the thread count and the algorithm.
- On 4.0, `CollectionBehavior(DisableTestParallelization = ...)`, `MaxParallelThreads`, and `ParallelAlgorithm` do not compile (CS0619: "Please set ParallelizationAttribute.Mode instead").
- Unit test assemblies keep the default parallel runs (TEST-17).

## Assertion libraries

- xUnit `Assert` needs no extra package. `Assert.Equal(expected, actual)`: the expected value is first.
- FluentAssertions 7.1 and later detect xUnit v3 (assembly `xunit.v3.assert`). 7.2.2 needs no bridge with xUnit. The FA 8 rule of `SKILL.md` applies.
- AwesomeAssertions detects xUnit v3 and needs no bridge.
- In 4.0, `Assert.All(collection, action, strict: true)` fails on an empty collection.

## Move from xUnit v2

Move only when the task asks or the human approves (TEST-20). Keep the test count: a move changes no test behavior.

1. Write down the test count of the v2 run.
2. Change each line of the table in each test project.
3. In `global.json`, select the runner of TEST-19 for each test project of the repository.
4. To check the move, run `dotnet build -warnaserror`, then `dotnet test --no-build`. Done when the build has 0 errors and the run passes with the test count of step 1.

| xUnit v2 | xUnit v3 4.x |
|----------|--------------|
| `<PackageReference Include="xunit" Version="2.9.3" />` | `<PackageReference Include="xunit.v3" Version="4.0.3" />` |
| `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` | No package on MTP. For VSTest: `xunit.v3.mtp-off` with both packages |
| Class library project | `<OutputType>Exe</OutputType>` |
| `net6.0` or `net7.0` | `net8.0` or later, or `net472` |
| `using Xunit.Abstractions;` | Remove it. `ITestOutputHelper` is in `Xunit`. |
| `Task InitializeAsync()` and `Task DisposeAsync()` of `IAsyncLifetime` | `ValueTask InitializeAsync()` and `ValueTask DisposeAsync()` |
| `async void` test | `async Task` or `async ValueTask` |
| Async calls without a token | `TestContext.Current.CancellationToken` (xUnit1051) |
| `[assembly: CollectionBehavior(DisableTestParallelization = true)]` | `[assembly: Parallelization(Mode = ParallelMode.None)]` with `using Xunit.Sdk;` and `using Xunit.v3;` |
| `CollectionBehavior(MaxParallelThreads = n)` | `Parallelization(MaxThreads = n)` |
| `CollectionBehavior(ParallelAlgorithm = ...)` | `Parallelization(Algorithm = ...)` |
| `ICollectionFixture` with `[Collection]` on each class, for one resource of the assembly | `[assembly: AssemblyFixture(typeof(...))]` |
| An `if` that returns early for a condition | `Assert.SkipUnless` or `Assert.SkipWhen` |

The Microsoft agent skill `migrate-xunit-to-xunit-v3` keeps the runner of the v2 project. It does not name the 4.0 parallel attributes, so check the `CollectionBehavior` lines after it.

## Traps

- `xunit.v3` 3.x is not the latest. Use 4.0.3.
- `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` without a `global.json` runner give the VSTest error on SDK 10 and SDK 11.
- An MTP runner in `global.json` makes each test project of the repository an MTP project. An MSTest project on VSTest in the same repository fails.
- Docs and samples for xUnit v2 (also the Reqnroll xUnit docs) show `Xunit.Abstractions.ITestOutputHelper`. In v3 the type is `Xunit.ITestOutputHelper`.
