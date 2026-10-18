# Review checklist

The full list for a review of C# code. `tool` means a tool finds it: the rule ID of `rules.json`, a compiler ID, or an analyzer ID. Analyzer IDs need the setup of `dev-stack:dotnet-analyzers`. `judge` means a reviewer reads the code. Run the tools first (stack rule 6).

## Contents

- Version and project files
- Types and nullability
- Async and time
- Errors and disposal
- Collections and strings
- Decisions and packages

## Version and project files

| # | Check | How |
|---|-------|-----|
| 1 | The code uses no C# feature newer than the target framework | tool (build) |
| 2 | No `LangVersion` in a project or props file | tool (CS-LANGVERSION) |
| 3 | `Directory.Build.props` turns on nullable | tool (CS-PROPS-NULLABLE) |
| 4 | CPM is on: no `Version` on `PackageReference` | tool (NU1008) |
| 5 | No floating package versions | tool (CS-FLOATING-VERSION, NU1011) |
| 6 | `global.json` pins the SDK | judge |

## Types and nullability

| # | Check | How |
|---|-------|-----|
| 7 | Classes are `sealed` unless designed for inheritance | tool (CA1852, internal types), judge (public types) |
| 8 | DTOs and messages are records. Value objects validate in the constructor and are immutable. | judge |
| 9 | A primary constructor parameter is not also copied into a field | tool (CS9124) |
| 10 | No `#nullable disable` in written code | tool (CS-NULLABLE-DISABLE) |
| 11 | Each new `!` has a proof in a comment. No `= null!` where `required` works. | judge |
| 12 | No redundant null checks inside trusted code. Input is checked at the boundary. | judge |
| 13 | Switch expressions over a hierarchy on `net10.0` have a discard arm that throws | judge |
| 14 | File-scoped namespaces, one top-level type per file | tool (IDE0161), judge |

## Async and time

| # | Check | How |
|---|-------|-----|
| 15 | No `async void`, except event handlers | tool (CS-ASYNC-VOID, VSTHRD100) |
| 16 | No `.GetAwaiter().GetResult()` on a task | tool (CS-GETRESULT) |
| 17 | No `.Result`, `.Wait()`, `Task.WaitAll`, `Task.WaitAny` on tasks | tool (CS-TASK-BLOCK, VSTHRD002) |
| 18 | Each async I/O method takes a `CancellationToken` last and passes it on | tool (CA2016), judge |
| 19 | Each task is awaited or stored with intent. No fire and forget. | tool (CS4014, VSTHRD110), judge |
| 20 | No `ConfigureAwait(false)` in app code. Present in each await of a library package. | tool (CA2007 in libraries), judge |
| 21 | `ValueTask` only on a measured hot path, awaited one time | judge |
| 22 | Fan-out over a list has a limit | judge |
| 23 | `TaskCompletionSource` uses `RunContinuationsAsynchronously` | tool (CS-TCS-OPTIONS) |
| 24 | Each created `CancellationTokenSource` is disposed | judge |
| 25 | `OperationCanceledException` is not logged as an error | judge |
| 26 | No `DateTime.Now`, `UtcNow`, `Today`, `DateTimeOffset.Now`, `UtcNow` outside tests | tool (CS-CLOCK, RS0030) |
| 27 | No `Thread.Sleep` outside tests | tool (CS-THREAD-SLEEP) |
| 28 | Delays, timers, and timeouts get the `TimeProvider` | judge |

## Errors and disposal

| # | Check | How |
|---|-------|-----|
| 29 | Expected outcomes are result values, not exceptions | judge |
| 30 | Argument checks use the throw helpers | tool (CA1510 to CA1513) |
| 31 | No `throw ex;` | tool (CA2200) |
| 32 | No empty catch, no catch of `Exception` below the top boundary | tool (RCS1075), judge |
| 33 | No secrets or personal data in exception messages | judge |
| 34 | Each created disposable is disposed. Injected objects are not disposed. | tool (CA2000 when on), judge |
| 35 | No finalizer without an unmanaged resource. Handles use `SafeHandle`. | judge |
| 36 | Suppressions have a rule ID and a reason | tool (CS-PRAGMA-NO-REASON), judge for the reason |

## Collections and strings

| # | Check | How |
|---|-------|-----|
| 37 | A LINQ query is enumerated one time | judge |
| 38 | `TryGetValue`, not `ContainsKey` and the indexer | tool (CA1854) |
| 39 | `Count`/`Length` property, not `Count()`. `Any()`, not `Count() > 0`. | tool (CA1827, CA1829) |
| 40 | Parameters accept the narrowest interface. Return values are read-only. | judge |
| 41 | Empty collections, not `null` | judge |
| 42 | Culture and comparison are explicit | tool (CA1304, CA1305, CA1309, CA1310, CA1311) |
| 43 | Fixed regex patterns use `[GeneratedRegex]`. Regex on user input has a timeout. | tool (SYSLIB1045), judge |

## Decisions and packages

| # | Check | How |
|---|-------|-----|
| 44 | No MediatR, no AutoMapper | tool (CS-PKG-MEDIATR, CS-PKG-AUTOMAPPER) |
| 45 | No `System.Linq.Async` on `net10.0` | tool (CS-PKG-LINQ-ASYNC) |
| 46 | Each new package has the approval of the human | judge |
| 47 | New code follows the accepted decisions (SOL-01 to SOL-09, CFG-04) | tool (`--detect` rules), judge |
| 48 | Interfaces only at a seam (SOL-09 default) | judge |
| 49 | Existing code keeps its style. Only the touched lines change. | judge |
