# Tests with the Orleans test cluster

The test framework, the runner, and the assertion library are in `test-stack:dotnet-testing`. This file covers the Orleans test cluster. The examples ran on SDK 11.0.100 with `MSTest.Sdk/4.5.3` and `Microsoft.Orleans.TestingHost` 10.4.2: 3 tests passed in about 3 seconds.

## Contents

- Project
- One cluster for each assembly
- One cluster for a test class
- What to test
- Traps

## Project

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Orleans.TestingHost" />
    <ProjectReference Include="../Shop.Silo/Shop.Silo.csproj" />
  </ItemGroup>
</Project>
```

The version of `Microsoft.Orleans.TestingHost` is in `Directory.Packages.props`, the same as the other Orleans packages. The `MSTest.Sdk` version is in `global.json` (`test-stack:dotnet-testing`).

## One cluster for each assembly

This is the default (ORL-09). The fixture is in `SKILL.md`, rule 26. The fixture class is `static`, else MSTEST0016 warns. Pass `context.CancellationToken` to `DeployAsync`, else MSTEST0049 warns.

- Give each test a new grain key: `$"page-{Guid.NewGuid():N}"`. Then tests can run in parallel (`[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`).
- An assembly has one `[AssemblyInitialize]`. When one exists (for example a container start), start the cluster there too.

## One cluster for a test class

Use this when a class needs other silo options, for example a fake service.

```csharp
[TestClass]
public sealed class PriceGrainTests
{
    private static InProcessTestCluster s_cluster = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext context)
    {
        var builder = new InProcessTestClusterBuilder(1);
        builder.ConfigureSilo((options, silo) =>
        {
            silo.AddMemoryGrainStorage("store");
            silo.Services.AddSingleton<IPriceSource>(new FakePriceSource(42m));
        });
        s_cluster = builder.Build();
        await s_cluster.DeployAsync(context.CancellationToken);
    }

    [ClassCleanup]
    public static async Task StopAsync() => await s_cluster.DisposeAsync();
}
```

The class form, with the fake service, passed in the same build check.

## What to test

- Each grain method that changes state: call it through `Cluster.Client`, then read the result through another call.
- A serialized type that changed: a round trip through a grain call shows that the serializer works.
- A stored state type that changed in a live service: a test with the real provider in a container. The test reads a record in the old shape. Memory storage starts empty, so it cannot show a defect with old records.
- A timer: call the callback logic through a grain method in the test. Do not wait for the period.

## Traps

- With memory storage, a write in `OnDeactivateAsync` fails at host stop: `No active nodes are compatible with grain memorystorage`. The host still stops with exit code 0. Test the write on deactivation with a real provider.
- `new TestClusterBuilder()` with an `ISiloConfigurator` class is the older form. `ISiloBuilderConfigurator` and `ConfigureApplicationParts` are Orleans 3.x and do not compile on 7 or later.
- Tests that share one grain key share its state, and fail only in a parallel run.
