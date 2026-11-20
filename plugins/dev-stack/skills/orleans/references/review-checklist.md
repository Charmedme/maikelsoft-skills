# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Hosting and packages
- Grains and serialization
- State
- Timers, reminders, and streams
- Concurrency
- Providers, observability, and preview features
- Tests
- Decisions
- Severity
- Do not flag

## Hosting and packages

| # | Check | Type |
|---|-------|------|
| 1 | The silo uses `UseOrleans`, a client without a silo uses `UseOrleansClient`. No `SiloHostBuilder` or `ClientBuilder` on 7 or later. | tool (ORL-HOST-3X) |
| 2 | All `Microsoft.Orleans.*` packages have one fixed version. No `*`, `10.*`, or open range. | tool (ORL-VERSION-FLOAT), judge |
| 3 | `ServiceId` and the provider names did not change. | judge |
| 4 | No localhost clustering and no memory storage in the production configuration. | judge |

## Grains and serialization

| # | Check | Type |
|---|-------|------|
| 5 | Each type that crosses the wire or goes into state has `[GenerateSerializer]`, `[Alias]`, and `[Id]` on each member. | judge |
| 6 | No `[Id]` of an existing member changed. The code does not use the `[Id]` of a removed member again. | judge |
| 7 | Each member of an existing state type keeps its name and its type. | judge |
| 8 | A grain interface change adds methods. A removed method or a changed parameter list waits for a second deploy. | judge |
| 9 | Each grain class keeps its name, or has `[GrainType]` with the old name. | judge |
| 10 | Grain interfaces and their methods have an `[Alias]`. | judge |
| 11 | Grain methods return `Task`, `ValueTask`, their generic forms, or `IAsyncEnumerable<T>`. | tool (compiler), judge |

## State

| # | Check | Type |
|---|-------|------|
| 12 | New grains inject `IPersistentState<T>` with `[PersistentState]`. No new `Grain<TState>`. | tool (ORL-GRAIN-TSTATE) |
| 13 | Each call that changes state writes it before it returns. Or a flush timer has an accepted loss window (ORL-04) in a comment and in the report. | judge |
| 14 | `WriteStateAsync` gets the `CancellationToken`. | judge |

## Timers, reminders, and streams

| # | Check | Type |
|---|-------|------|
| 15 | Timers use `RegisterGrainTimer`. A migrated `RegisterTimer` call sets `Interleave = true`, or the review explains `false`. | tool (ORL-TIMER-OLD, CS0618), judge |
| 16 | Reminders have a period of 1 minute or more, and the grain unregisters a reminder that is no longer needed. | judge |
| 17 | Streams use `StreamId.Create`. Memory streams are only in tests. | judge |
| 18 | Messages to other services go through a broker, not Orleans streams. | judge |

## Concurrency

| # | Check | Type |
|---|-------|------|
| 19 | No `ConfigureAwait(false)` in grain code. | tool (ORLEANS0014, ORL-CONFIGUREAWAIT) |
| 20 | No `.Result`, `.Wait()`, `GetAwaiter().GetResult()`, or `Thread.Sleep` in grain code. | judge |
| 21 | A `CancellationToken` parameter is last in each new grain method. | tool (ORLEANS0113) |
| 22 | `[Reentrant]` has a reason in a comment. `[AlwaysInterleave]` is on the interface method. | tool (Orleans analyzer for `[AlwaysInterleave]`), judge |
| 23 | No call cycle between grains that are not reentrant. | judge |

## Providers, observability, and preview features

| # | Check | Type |
|---|-------|------|
| 24 | Each provider package has the same version as the other Orleans packages. ADO.NET providers have the SQL scripts of that version. | judge |
| 25 | Metrics use `OrleansInstruments` from DI, not `Orleans.Runtime.Instruments.Meter`. | tool (compiler on 10.2 and later) |
| 26 | `AddActivityPropagation()` on the silo and the client when the service has traces. | judge |
| 27 | The Dashboard route has `RequireAuthorization` with a policy. | judge |
| 28 | Each alpha package and each `ORLEANSEXP` API has an accepted decision and a suppression on the line, not a project-wide `NoWarn`. | tool (`ORLEANSEXP`), judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 29 | New tests use `InProcessTestClusterBuilder` with delegates. | tool (ORL-TEST-OLD) |
| 30 | Each test uses a new grain key, or the class does not run in parallel. | judge |
| 31 | A test with a real provider, not memory storage, checks a write on deactivation. | judge |
| 32 | A changed stored state type has a test that reads a record in the old shape. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 33 | New Orleans code follows the accepted ORL decisions, or the output marks them as assumed. | judge |

## Severity

In a live service, these findings are blocking:

- a changed `[Id]`, or a renamed member of a state type
- a removed grain method in one deploy
- a renamed grain class without `[GrainType]`
- a changed `ServiceId`
- `ConfigureAwait(false)` or a blocking call in grain code
- memory storage in production

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `Grain<TState>` in an existing grain that the task does not move (`existing-code.md`).
- ORLEANS0113 suppressed on an existing grain method with the reason "callers on the old version".
- `TestClusterBuilder` in an existing test class.
- `RegisterGrainTimer` with `Interleave = true` in a grain that came from `RegisterTimer`.
- `UseLocalhostClustering` and `AddMemoryGrainStorage` in a test project or in a development-only branch of `Program.cs`.
