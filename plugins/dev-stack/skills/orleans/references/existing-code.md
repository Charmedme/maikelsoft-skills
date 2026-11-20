# Existing code on Orleans 3.x, 7, 8, 9, and 10

A live cluster has stored state and callers on the current version. Existing code keeps its style (stack rule 3). This file says what to keep, what to change when the task touches the code, and what an upgrade changes.

## Contents

- How to work in a live service
- Keep, or change when touched
- Defects to fix
- Orleans 7, 8, and 9
- Changes in 10.1 to 10.4
- Orleans 3.x

## How to work in a live service

1. Read the version of each `Microsoft.Orleans.*` package. Each package must have the same version.
2. Read `ClusterOptions`, the provider names, and the storage serializer. Do not change them in a feature task.
3. Before you change a serialized type, a state type, or a grain interface, read the change table in `grains.md`.
4. Add a test-cluster test for the grain that you change, also when the grain had no test. Existing tests with `TestClusterBuilder` stay as they are.

Done when the report says for each changed type or interface why old and new silos can run side by side.

## Keep, or change when touched

| Existing form | Versions | What to do |
|---------------|----------|------------|
| `class X : Grain<TState>` | 3.x to 10 | Keep. It compiles in 10.4.2 with no warning. A move to `IPersistentState<T>` can read from another storage key, and then the state looks empty. Move only as its own task: read a stored record in a test environment first, and set the same state name and provider. |
| `RegisterTimer(callback, state, due, period)` | 3.x to 10, CS0618 since 8.2 | When the task edits the grain: `this.RegisterGrainTimer(callback, state, new() { DueTime = due, Period = period, Interleave = true })`. `Interleave = true` keeps the old behavior. |
| `GrainCancellationToken` parameters | 3.x to 8 | Keep in existing methods. New methods on 9 or later take a `CancellationToken`. |
| `CancellationToken` that is not the last parameter | 9 to 10, ORLEANS0113 since 10.4 | Keep the order in an existing method, and suppress ORLEANS0113 on it with the reason "callers on the old version". A new order can break those callers (from the 10.4 note that parameter `[Id]` values set the wire IDs, not verified by test). |
| `TestClusterBuilder` with `ISiloConfigurator` classes | 7 to 10 | Keep the existing tests. New test classes use `InProcessTestClusterBuilder`. |
| `OrleansDashboard` package with `UseDashboard()` | 3.x to 8 | Keep on 8.x. On 10, the package has no release: ask to replace it with `Microsoft.Orleans.Dashboard` (a package change, stack rule 5). |
| No `[Alias]` on types or grain interfaces | 7 to 10 | Keep the current names. Add `[Alias]` with the current full type name when you edit the type, so a later rename is safe. |
| `[Serializable]` with `[GenerateSerializer]` on 7 or later | 7 to 10 | Keep `[GenerateSerializer]` and `[Id]`. `[Serializable]` alone does nothing for the Orleans serializer on 7 or later. |

## Defects to fix

These forms are bugs, not style. When the change calls, copies, or edits the code, fix them and name the fix under "Changes". Else report them under "Open points".

| Defect | Why |
|--------|-----|
| `ConfigureAwait(false)` in grain code (ORLEANS0014 since 10.1) | The rest of the method runs off the grain scheduler, so two threads can change the state at once. |
| `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, `Thread.Sleep` in a grain | They block the grain scheduler. No analyzer flags them. |
| A serialized type on 7 or later without `[GenerateSerializer]` or without `[Id]` | The call fails at run time, or a member is not sent. |
| `AddMemoryGrainStorage` or `UseLocalhostClustering` in the production configuration | A restart loses the state. Each silo forms its own cluster. |
| A floating `Microsoft.Orleans.*` version (`*`, `10.*`) | A restore can change the silo version without a commit. Pin the version that restores today, not a newer one. |
| Packages of different Orleans versions in one silo | The silo can fail at start or at run time. |
| A flush timer with no word on the loss window | The team does not know that a crash loses data (ORL-04). |

## Orleans 7, 8, and 9

- 7.0 replaced the serializer (`[GenerateSerializer]`, `[Id]`) and the grain identity. 7 and later do not need `ConfigureApplicationParts`.
- 8.2 made `RegisterTimer` obsolete and added `RegisterGrainTimer`.
- 9.0 added full `CancellationToken` support in grain methods, `InProcessTestCluster`, the strong-consistency grain directory, and memory-based activation shedding. 9.2 made `ResourceOptimizedPlacement` the default.
- Old names: `AddGrainCallFilter` is now `AddIncomingGrainCallFilter`. `[Unordered]` is gone. `OrleansConstructorAttribute` is now `GeneratedActivatorConstructorAttribute`.
- Upgrade within 7 to 10 only when the task asks. Read the release notes of each minor version between the two versions, and test a rolling deploy in a test environment.

## Changes in 10.1 to 10.4

| Version | Change that affects code |
|---------|--------------------------|
| 10.0 | Orleans Dashboard (`Microsoft.Orleans.Dashboard`). Redis providers for clustering, persistence, and reminders are stable. |
| 10.1 | ORLEANS0014 warns about `ConfigureAwait` in grain code. ADO.NET persistence supports SQLite. `JournaledGrain` gets `ClearLogAsync`. |
| 10.2 | The static meter `Orleans.Runtime.Instruments.Meter` is gone. Inject `OrleansInstruments`. JSON Lines is the default journaling format. |
| 10.3 | The default Newtonsoft.Json grain storage serializer resolves `$type` only for allowed types. A System.Text.Json grain storage serializer exists. Activities use the key `rpc.system.name`. `IConfigureGrainContext` runs before the grain constructor. |
| 10.4 | ORLEANS0113: a `CancellationToken` parameter goes last. Parameter `[Id]` attributes set the wire IDs. The latency metric is one histogram `orleans-app-requests-latency`. The `orleans-grains` metric uses the tag `grain_type`. Apply the SQLite scripts again. |

After an upgrade to 10.3, check the stored state that has `$type` values. A type that is not allowed does not load.

## Orleans 3.x

A 3.x code base uses `SiloHostBuilder`, `ClientBuilder`, `ConfigureApplicationParts`, `[Serializable]`, and `ISiloBuilderConfigurator` in tests. Most code of this skill does not compile there.

- Keep the 3.x forms in a 3.x code base. Do not mix 7 or later APIs into it.
- A move from 3.x to 7 or later is a project of its own. The serializer and the grain identity changed, so old and new silos cannot form one cluster, and stored state needs a check. Do not start it as part of another task. Tell the human, and give the migration guide as the source.
- Tests on 3.x use `TestClusterBuilder` with `ISiloBuilderConfigurator`. `InProcessTestClusterBuilder` does not exist there.
