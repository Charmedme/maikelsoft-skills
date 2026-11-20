---
name: orleans
description: Orleans 10 code in .NET - silo and client hosting, grains and grain interfaces, serialization and version-safe changes, persistent state, timers and reminders, streams, reentrancy, clustering and storage providers, placement, the Dashboard, Orleans metrics, and tests with InProcessTestCluster and MSTest. Also changes to existing grains on Orleans 7, 8, 9, or 3.x. Use when code or a review touches UseOrleans, UseOrleansClient, IGrainFactory, a grain or a grain interface (IGrainWithStringKey, Grain, IGrainBase), GenerateSerializer, Id or Alias attributes, IPersistentState, Grain of TState, RegisterGrainTimer, RegisterTimer, reminders, Orleans streams, Reentrant, a test cluster, or a Microsoft.Orleans package. Not for the Aspire AppHost side (dev-stack:aspire), the Redis, PostgreSQL, or EF Core store itself (dev-stack:redis, dev-stack:postgresql, dev-stack:efcore), the test framework (test-stack:dotnet-testing), OpenTelemetry setup (dev-stack:dotnet-observability), Akka.NET, or Dapr actors.
metadata:
  verified: "2026-11-19"
  versions: "Microsoft.Orleans.* 10.4.2 (net8.0 and net10.0 assets), .NET 10 and .NET 11 (silo and test cluster verified by build on SDK 11.0.100), MSTest.Sdk 4.5.3"
---

# Orleans

Virtual actors (grains) in .NET with Orleans 10: the silo and the client, grain code, serialization, state, timers, reminders, streams, providers, and tests. A live cluster with stored state needs the most care. During a rolling deploy, old and new silos run side by side. Each change to a grain interface, a serialized type, or a state type must work in that period.

The AppHost side (`AddOrleans`, `WithClustering`) is in `dev-stack:aspire`. The store engines are in `dev-stack:redis`, `dev-stack:postgresql`, and `dev-stack:efcore`. The test framework is in `test-stack:dotnet-testing`. This skill owns the test cluster.

## First

1. Read `references/stack-rules.md`.
2. Find the Orleans version: the `Microsoft.Orleans.*` packages in `*.csproj` or `Directory.Packages.props`, and the target framework.
   - 10.x: use this file.
   - 7.x to 9.x: use this file, and read `references/existing-code.md` before you change a grain. Do not upgrade unless the task asks.
   - 3.x (`Microsoft.Orleans.Core` 3.x, `SiloHostBuilder`, `[Serializable]`): read `references/existing-code.md` first. Most rules of this file do not compile on 3.x.
   - A floating version (`*`, `10.*`): rule 3.
3. Find out if the cluster runs in production with stored state. Read the `ServiceId`, the provider names (`"store"`), and the grain storage serializer. When the cluster is live, rules 8 to 11 apply to each change.

Done when you can name the Orleans version, the target framework, and each provider (clustering, storage, reminders, streams). You also know if the cluster has stored state in production.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Hosting | Silo in the same process as the API (`UseOrleans`). A separate client only when the front end must scale apart. | ORL-01 |
| Clustering | The store that the service already runs: Redis, else ADO.NET on PostgreSQL. Localhost only for development and tests. | ORL-02 |
| Grain storage | Same store as the clustering. Keep the default storage serializer of the provider. | ORL-03 |
| State writes | Write in each call that changes state. A flush timer only with an accepted loss window. | ORL-04 |
| Scheduled work | Grain timer while active. Reminder (1 minute or more) when the work must survive deactivation. No Durable Jobs (alpha). | ORL-05 |
| Reentrancy | Not reentrant. `[ReadOnly]` on read methods. `[Reentrant]` only with a reason. | ORL-06 |
| Streams | No streams until a need. Memory streams only in tests. Messages to other services go through a broker. | ORL-07 |
| Dashboard | `Microsoft.Orleans.Dashboard` on an internal route with `RequireAuthorization()`. | ORL-08 |
| Test cluster | One `InProcessTestCluster` for each test assembly, a new grain key for each test. | ORL-09 |
| Target framework | `net10.0` (LTS). `net11.0` when the solution already targets it. | ORL-10 |

## Existing code

Existing grains keep their style (stack rule 3). Change these forms only when the task edits that grain or that test. The full table, with the 3.x and 7 to 9 notes, is in `references/existing-code.md`.

| Existing form | Keep or change |
|---------------|----------------|
| `class X : Grain<TState>` | Keep. A move to `IPersistentState<T>` can change the storage key. Do it only as its own task, after you read a stored record. |
| `RegisterTimer(...)` (CS0618) | When you edit the grain: `RegisterGrainTimer` with `Interleave = true`. That keeps the old behavior. Use `false` only after you check the callback. |
| `CancellationToken` not last (ORLEANS0113) | Keep the order of an existing method: a new order can break callers on the old silo version. Suppress with a reason. New methods put it last. |
| `TestClusterBuilder` with `ISiloConfigurator` | Keep the existing tests. New test classes use `InProcessTestClusterBuilder`. |
| `OrleansDashboard` package (OrleansContrib, 8.2 and older) | Keep on 8.x. On 10.x, ask to replace it with `Microsoft.Orleans.Dashboard`. |

These are defects, not style. Fix them when the change calls, copies, or edits the code. Else report them (stack rule 3):

- `ConfigureAwait(false)` or a blocking call in a grain
- a serialized type without `[Id]`
- memory storage in the production configuration
- a floating Orleans version

## Rules

### Hosting and packages

1. Host the silo with `UseOrleans` on `WebApplicationBuilder` or `HostApplicationBuilder`. Host a client without a silo with `UseOrleansClient`. A co-hosted API injects `IGrainFactory` and needs no client.
2. Set `ClusterOptions.ServiceId` one time and never change it. Providers use it in the keys of stored state, so a new value starts with empty state. `ClusterId` names one deployment of the cluster.
3. Give all `Microsoft.Orleans.*` packages one fixed version in `Directory.Packages.props`, for example `10.4.2`. A floating version (`*`, `10.*`) changes the build without a commit. When you pin an existing floating version, use the version that restores today, not a newer one.
4. A silo can target `net11.0` with Orleans 10.4.2. The package has only `net8.0` and `net10.0` assets, but a build and an `InProcessTestCluster` test passed on SDK 11.0.100.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.UseOrleans(silo =>
{
    silo.Configure<ClusterOptions>(o => { o.ClusterId = "prod"; o.ServiceId = "shop"; }); // never change ServiceId
    silo.UseRedisClustering(o => o.ConfigurationOptions = ConfigurationOptions.Parse(redis));
    silo.AddRedisGrainStorage("store", o => o.ConfigurationOptions = ConfigurationOptions.Parse(redis));
});
var app = builder.Build();
app.MapPost("/carts/{id}/items", (string id, AddItem body, IGrainFactory grains, CancellationToken ct) =>
    grains.GetGrain<ICartGrain>(id).AddItemAsync(body.Sku, body.Quantity, ct));
```

### Grains and interfaces

5. Derive a grain from `Grain`, or implement `IGrainBase` with an `IGrainContext GrainContext` property (a POCO grain). Each grain method returns `Task`, `Task<T>`, `ValueTask`, `ValueTask<T>`, or `IAsyncEnumerable<T>`. A grain interface has no properties.
6. Put a `CancellationToken` parameter last in each grain method (ORLEANS0113 since 10.4). Pass it to `WriteStateAsync` and to each I/O call.
7. Give the grain interface and each of its methods an `[Alias]`. Then a rename of the C# name does not change the wire name.

### Serialization and version-safe changes

8. Mark each type that crosses the wire or goes into state with `[GenerateSerializer]`, `[Alias("Namespace.Type")]`, and `[Id(n)]` on each member. A record uses `[property: Id(0)]` on each parameter. `[Serializable]` is the 3.x form.
9. Keep each `[Id]` and each `[Alias]` for the life of the type. Add a member with a new `[Id]`. Never reuse the `[Id]` of a removed member. A changed `[Id]` or member type breaks messages between old and new silos, and state that the binary serializer stored.
10. Keep the member names of a state type too. The default grain storage serializer of most providers is JSON, which uses names, not `[Id]`. A renamed property reads as its default value, with no error.
11. Change a grain interface in two deploys. First add the new method and keep the old one. Remove the old method only after no silo or client runs the old version. Keep the class name of a grain, or set `[GrainType("old-name")]`: the grain type is part of the storage key.

The full table of safe and unsafe changes is in `references/grains.md`.

### State

12. Inject state with a constructor parameter `[PersistentState("cart", "store")] IPersistentState<CartState> state`. Do not write `Grain<TState>` in new code. The docs call it legacy, but the build gives no warning, so stackcheck flags it (ORL-GRAIN-TSTATE).
13. Call `WriteStateAsync(cancellationToken)` in each call that changes state, before it returns. When a flush timer writes instead (ORL-04), write the loss window in the code and in the report. Example: "a silo crash loses the changes since the last write". A write in `OnDeactivateAsync` does not run after a crash.
14. Test a write in `OnDeactivateAsync` with a real storage provider. With memory storage, the write fails at host stop, because the storage grain stops first.

### Timers, reminders, and streams

15. Use `this.RegisterGrainTimer(callback, new GrainTimerCreationOptions { DueTime, Period })` for work while the grain is active. The callback gets a `CancellationToken`. `Interleave` is `false` by default, so the callback waits for the running call. Dispose the returned `IGrainTimer` to stop it.
16. Use a reminder (`IRemindable`, `this.RegisterOrUpdateReminder`) only for work that must run when the grain is not active. Give it a period of 1 minute or more. A reminder writes to storage on each registration, and it is not for ticks of seconds.
17. Get a stream with `this.GetStreamProvider("events").GetStream<T>(StreamId.Create("cart", key))`. Memory streams lose each event at a silo crash. Messages to other services go through a broker (`dev-stack:rabbitmq`, `dev-stack:wolverine`).

### Concurrency

18. A grain runs one call at a time. Await each task. Do not use `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, or `Thread.Sleep` in grain code. No analyzer flags them, and they block the grain scheduler.
19. Write a plain `await` in grain code. `ConfigureAwait(false)` moves the rest of the method off the grain scheduler (ORLEANS0014). Then two threads can change the state at once.
20. Mark a method that only reads `[ReadOnly]`. Put `[AlwaysInterleave]` on the interface method, not on the class. Use `[Reentrant]` on a class only with a reason in a comment. In a reentrant grain, the state can change at each `await`.

### Providers, placement, observability, and preview features

21. Select providers by package (ORL-02, ORL-03). The table of packages and calls is in `references/hosting.md`. The ADO.NET providers need the SQL scripts of the package in the database.
22. Keep the default placement (`ResourceOptimizedPlacement` since 9.2). Use `[PreferLocalPlacement]`, `[HashBasedPlacement]`, or `[Immovable]` only for a measured need.
23. Get the Orleans meter from DI: `OrleansInstruments` (namespace `Orleans.Runtime`). The static `Orleans.Runtime.Instruments.Meter` is gone since 10.2. Add the meter `Microsoft.Orleans` and the sources `Microsoft.Orleans.Runtime` and `Microsoft.Orleans.Application` to OpenTelemetry. Call `AddActivityPropagation()` on the silo and the client. The OpenTelemetry setup is in `dev-stack:dotnet-observability`.
24. Map the Dashboard with `app.MapOrleansDashboard().RequireAuthorization()` after `silo.AddDashboard()` (namespace `Orleans.Dashboard`). It shows grain names, silo addresses, and call data, so it is not for anonymous users. The docs mark it preview.
25. Use a preview feature only with an accepted decision and a reason in the code. The preview features are the `10.4.2-alpha.1` packages (Durable Jobs, Journaling, NATS streams) and each API with an `ORLEANSEXP` diagnostic. `ORLEANSEXP001` (activation repartitioning) is an error until you suppress it. Suppress only that ID, on that line, with the reason.

### Tests

26. Test grains in an `InProcessTestCluster`. Start one cluster for each test assembly in `[AssemblyInitialize]`. Give each test a new grain key, so parallel tests do not share state. The MSTest project setup is in `test-stack:dotnet-testing`. More cases are in `references/testing.md`.

```csharp
[TestClass]
public static class ClusterFixture
{
    public static InProcessTestCluster Cluster { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        var builder = new InProcessTestClusterBuilder(1);
        builder.ConfigureSilo((options, silo) => silo.AddMemoryGrainStorage("store"));
        Cluster = builder.Build();
        await Cluster.DeployAsync(context.CancellationToken);
    }

    [AssemblyCleanup]
    public static async Task StopAsync() => await Cluster.DisposeAsync();
}

[TestClass]
public sealed class ViewCounterGrainTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task AddViewAsync_TwoCalls_ReturnsTwo()
    {
        var grain = ClusterFixture.Cluster.Client.GetGrain<IViewCounterGrain>($"page-{Guid.NewGuid():N}");

        await grain.AddViewAsync(TestContext.CancellationToken);
        var count = await grain.AddViewAsync(TestContext.CancellationToken);

        Assert.AreEqual(2L, count);
    }
}
```

## Never write

| Wrong | Correct |
|-------|---------|
| `new SiloHostBuilder()`, `new ClientBuilder()`, `ConfigureApplicationParts(...)` | `builder.UseOrleans(...)` or `builder.UseOrleansClient(...)`. Orleans 7 and later find grains without application parts. |
| `public class CounterGrain : Grain<CounterState>` in new code | `: Grain` with `[PersistentState("counter", "store")] IPersistentState<CounterState>` |
| `RegisterTimer(cb, null, due, period)` | `this.RegisterGrainTimer(cb, new GrainTimerCreationOptions { DueTime = due, Period = period })` |
| `await state.WriteStateAsync().ConfigureAwait(false);` in a grain | `await state.WriteStateAsync(cancellationToken);` |
| `[Serializable]`, or no attribute, on a message or state type | `[GenerateSerializer, Alias("Shop.CartLine")]` and `[Id(n)]` on each member |
| `new TestClusterBuilder()` with an `ISiloConfigurator` or `ISiloBuilderConfigurator` class | `new InProcessTestClusterBuilder(1)` with `ConfigureSilo((options, silo) => ...)` |
| `<PackageReference Include="Microsoft.Orleans.Server" Version="*" />` or `"10.*"` | `<PackageVersion Include="Microsoft.Orleans.Server" Version="10.4.2" />` in `Directory.Packages.props` |
| `OrleansDashboard` package with `UseDashboard()` | `Microsoft.Orleans.Dashboard`, `AddDashboard()`, `MapOrleansDashboard().RequireAuthorization()` |
| `GetStream<T>(guid, "cart")` | `GetStream<T>(StreamId.Create("cart", key))` |
| `Orleans.Runtime.Instruments.Meter` | `OrleansInstruments` from DI |
| `Task AddAsync(CancellationToken ct, int quantity)` in a new method | `Task AddAsync(int quantity, CancellationToken cancellationToken = default)` |
| A reminder with a period of seconds | A grain timer |
| A flush timer with no word on data loss | A comment and a report line: a crash loses the changes since the last write |
| `AddMemoryGrainStorage` in the production configuration | A real provider (ORL-03) |
| `silo.AddActivationRepartitioner();` with `<NoWarn>ORLEANSEXP001</NoWarn>` for the project | No experimental feature, or an accepted decision and a suppression on that line only |
| `AddGrainCallFilter(...)` | `AddIncomingGrainCallFilter(...)` |

## Check

1. Build with warnings as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings. ORLEANS0014 finds `ConfigureAwait(false)`, ORLEANS0113 a `CancellationToken` that is not last, CS0618 `RegisterTimer`, and `ORLEANSEXP` diagnostics an experimental API. No diagnostic finds `Grain<TState>` or a blocking call.

2. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`. In existing code, add `--changed <base branch>`.

3. Run the tests: `dotnet test`. Pass: all tests green, with a test-cluster test for each changed grain.

## Review checklist

The full list is in `references/review-checklist.md`.

1. Hosting with `UseOrleans` or `UseOrleansClient`, a fixed Orleans version, a stable `ServiceId`. tool (ORL-HOST-3X, ORL-VERSION-FLOAT), judge
2. Each serialized type has `[GenerateSerializer]`, `[Alias]`, and `[Id]`. No `[Id]` or member name of an existing type changed. judge
3. Grain interface changes work while old and new silos run. judge
4. New grains use `IPersistentState<T>`, not `Grain<TState>`. tool (ORL-GRAIN-TSTATE)
5. The call that changes state writes it, or the report names the loss window. judge
6. Timers use `RegisterGrainTimer`. Reminders have a period of 1 minute or more. tool (ORL-TIMER-OLD, CS0618), judge
7. No `ConfigureAwait(false)` and no blocking call in grain code. tool (ORLEANS0014, ORL-CONFIGUREAWAIT), judge
8. `CancellationToken` last in new grain methods, and passed on. tool (ORLEANS0113), judge
9. Reentrancy attributes have a reason. judge
10. The Dashboard route requires authorization. judge
11. Preview features have an accepted decision and a suppression on the line. tool (`ORLEANSEXP`), judge
12. Tests use `InProcessTestClusterBuilder` and a new grain key for each test. tool (ORL-TEST-OLD), judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Changes": each changed serialized type, state type, or grain interface. Say why the change works while old and new silos run (rules 8 to 11).
- Under "Assumptions" or "Open points": the loss window of each flush timer (rule 13).
- Under "Decisions": each ORL decision that the change uses. Mark each one accepted or assumed.

## References

- `references/decisions.md`: read before new code needs an ORL decision. It has the question, the options, and the recommendation.
- `references/grains.md`: read when you change a serialized type, a state type, or a grain interface. Also read it for timers, reminders, streams, observers, or reentrant code.
- `references/hosting.md`: read when you set up a silo or a client, select a provider, add the Dashboard or metrics, or consider a preview feature.
- `references/existing-code.md`: read before you change code on Orleans 3.x, 7, 8, or 9, or before an upgrade.
- `references/testing.md`: read before you write a test-cluster test.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the build checks.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
