# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by build and test
- Not verified

## Versions

| Item | Version on 2026-11-19 | Source |
|------|-----------------------|--------|
| Microsoft.Orleans.* | 10.4.2 (2026-10-30), assets `net8.0` and `net10.0` | [Orleans releases](https://github.com/dotnet/orleans/releases), nuget.org |
| `Microsoft.Orleans.DurableJobs`, `Microsoft.Orleans.Journaling`, `Microsoft.Orleans.Streaming.NATS` | 10.4.2-alpha.1 | nuget.org |
| Microsoft.Orleans.Dashboard | 10.4.2, no preview tag. The docs say preview. | nuget.org, [Orleans Dashboard](https://learn.microsoft.com/dotnet/orleans/dashboard/) |
| OrleansDashboard (OrleansContrib) | 8.2.0, the last release | nuget.org |
| MSTest.Sdk | 4.5.3 | nuget.org |
| .NET SDK | 11.0.100 (.NET 11 GA on 2026-11-10) | [.NET 11 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/11.0/releases.json) |

## Documentation

- [Orleans overview](https://learn.microsoft.com/dotnet/orleans/overview): supported versions, the 3.x legacy note.
- [Migration guide](https://learn.microsoft.com/dotnet/orleans/migration-guide): 7, 8, 9, and 10 changes, renamed APIs.
- [Serialization](https://learn.microsoft.com/dotnet/orleans/host/configuration-guide/serialization): `[GenerateSerializer]`, `[Id]`, `[Alias]`, and versions of types.
- [Grain persistence](https://learn.microsoft.com/dotnet/orleans/grains/grain-persistence/): `IPersistentState<T>`, `Grain<TState>` as legacy.
- [Timers and reminders](https://learn.microsoft.com/dotnet/orleans/grains/timers-and-reminders): `RegisterGrainTimer`, the migration from `RegisterTimer`, reminder periods.
- [Streaming APIs](https://learn.microsoft.com/dotnet/orleans/streaming/streams-programming-apis): `StreamId.Create`.
- [Grain placement](https://learn.microsoft.com/dotnet/orleans/grains/grain-placement): defaults, attributes, ORLEANSEXP001 and ORLEANSEXP002.
- [Grain lifecycle](https://learn.microsoft.com/dotnet/orleans/grains/grain-lifecycle#grain-migration): `[Immovable]`, `MigrateOnIdle`.
- [Develop a grain](https://learn.microsoft.com/dotnet/orleans/grains/#iasyncenumerable-return-values): `IAsyncEnumerable<T>` return values.
- [Unit testing](https://learn.microsoft.com/dotnet/orleans/implementation/testing): `InProcessTestCluster`.
- [Aspire Orleans integration](https://aspire.dev/integrations/frameworks/orleans/): the AppHost and silo wiring.
- Release notes [10.1.0](https://github.com/dotnet/orleans/releases/tag/v10.1.0) and [10.4.0](https://github.com/dotnet/orleans/releases/tag/v10.4.0): ORLEANS0014, the meter change, the `CancellationToken` order, parameter `[Id]` wire IDs.

## Checked by build and test

On 2026-11-19 with SDK 11.0.100, Orleans 10.4.2, and `net11.0`. The phase 2 compile check (`docs/work/stack-split/compile-check.md` in the source repository) and a second check for this skill:

| Fact | Result |
|------|--------|
| A silo on `net11.0` builds and runs. An `InProcessTestCluster` test passes. | Pass |
| `[PersistentState]`, `RegisterGrainTimer`, `[GenerateSerializer]`, `[Id]`, `[Alias]` | Compile and run |
| `RegisterTimer` | CS0618, with the migration text that sets `Interleave = true` |
| `Grain<TState>` | Compiles with no warning |
| `ConfigureAwait(false)` in a grain | ORLEANS0014 |
| `CancellationToken` as the first parameter of a grain method | Warning ORLEANS0113 (the token goes last) |
| `.Result` in a grain method | No warning |
| `AddActivationRepartitioner()` | Error ORLEANSEXP001 until suppressed |
| A write in `OnDeactivateAsync` with memory storage at host stop | Fails: "No active nodes are compatible with grain memorystorage". Exit code 0. |
| `OrleansInstruments` | In DI, namespace `Orleans.Runtime`, property `Meter`, constructor takes `IMeterFactory` |
| `AddDashboard()`, `MapOrleansDashboard().RequireAuthorization()` | Compile with `using Orleans.Dashboard;` |
| POCO grain with `IGrainBase`, `[Reentrant]`, `[ReadOnly]`, `[AlwaysInterleave]` on the interface method | Compile |
| Record with `[property: Id(0)]`, `IAsyncEnumerable<T>` return, `ObserverManager<T>`, reminders, `StreamId.Create`, `[ImplicitStreamSubscription]` | Compile, and the grain call runs |
| `UseRedisClustering`, `AddRedisGrainStorage`, `UseRedisReminderService`, `UseAdoNetClustering`, `AddAdoNetGrainStorage`, `UseAdoNetReminderService` | Compile |
| `AddActivityPropagation()` on `ISiloBuilder` and `IClientBuilder` | In the XML docs of 10.4.2 |
| MSTest fixture with `[AssemblyInitialize]` on a static class, and a class fixture with `[ClassInitialize]` | 3 tests pass, 0 MSTest analyzer warnings |

## Not verified

- The storage key that `Grain<TState>` uses compared to `[PersistentState]`. The skill treats a move as unsafe until a stored record shows the key.
- That a new parameter order breaks callers on the old version. The skill takes it from the 10.4 note on parameter `[Id]` wire IDs.
- That a grain class rename changes the storage key. From the grain identity docs.
- The Azure Storage provider method names.
- The silo side of the Aspire integration with Aspire 17.
- The current Dashboard status: the docs say preview, NuGet has no preview tag.
- The API detail of `JournaledGrain`, Durable Jobs, and the System.Text.Json grain storage serializer.
