---
name: csharp
description: C# code and project conventions for .NET 10 and C# 14, with a reference for .NET 11 and C# 15. Covers nullability, records, pattern matching, async and cancellation, disposal, exceptions, TimeProvider, collections and LINQ, naming, Directory.Build.props, Central Package Management, and global.json. Use when writing, reviewing, or refactoring C# in any .NET project, when editing .csproj or props files, or when fixing async, null, or disposal bugs. Not for host, DI, endpoints, options, or HttpClient setup (dev-stack:aspnetcore), analyzer severities (dev-stack:dotnet-analyzers), test code (dev-stack:dotnet-testing), Unity C#, or .NET Framework 4.x.
metadata:
  verified: "2026-10-27"
  versions: ".NET 10 (runtime 10.0.14, SDK 10.0.403), C# 14. Reference for .NET 11 RC 3 and C# 15."
---

# C#

Rules for the C# language and for code that compiles in a plain class library. Code that needs `IServiceCollection`, `IHost`, `HttpContext`, or an endpoint: `dev-stack:aspnetcore`. Analyzer packages and severities: `dev-stack:dotnet-analyzers`.

## First

1. Read `references/stack-rules.md`. It has the common rules of all stack skills.
2. Find the target framework. Read `TargetFramework` or `TargetFrameworks` in the `.csproj` and in `Directory.Build.props`. Read the SDK version in `global.json`. Done when you can name the C# version:

   | Target | C# | Read |
   |--------|----|------|
   | `net10.0` | 14 | this file |
   | `net11.0` | 15 | this file, then `references/net11.md` |
   | `net9.0` | 13 | this file. Do not use the C# 14 features in `references/language-features.md`. |
   | `net8.0` | 12 | this file. No C# 13 or 14 features (also no `params` collections). |

3. A project with more than one target framework: write for the lowest C# version of all targets.

## Defaults for new code

Existing code keeps its style (stack rule 3). For new code, these are the recommendations. Ask the human when no accepted decision exists (stack rule 4).

| Area | Default | Decision |
|------|---------|----------|
| SDK pin | `global.json`: `10.0.403`, `rollForward: latestFeature` | SOL-05 |
| Solution shape | One project, feature folders | SOL-01, SOL-02 |
| Build policy | `Directory.Build.props`: `net10.0`, `Nullable`, `ImplicitUsings`, `AnalysisLevel latest-recommended`, no `LangVersion` | SOL-03 |
| Warnings | Nullable warnings are errors. All warnings are errors in CI. | SOL-06 |
| Code style | Template `.editorconfig`, file-scoped namespaces at `warning` | SOL-07 |
| Names | `_camelCase`, `s_camelCase`, `Async` suffix | SOL-08 |
| Packages | Central Package Management, transitive pinning in apps | SOL-04 |
| Interfaces | Only at a seam (I/O, more than one implementation) | SOL-09 |
| Time | `TimeProvider`, instants as UTC `DateTimeOffset` | CFG-04 |
| Classes | `sealed` unless designed for inheritance | rule 1 |

## Rules

### Types and data

1. Make each class `sealed` unless you design it for inheritance. The intent is clear, and the JIT can remove virtual calls.
2. Use `sealed record` for DTOs, messages, and value objects. Use `readonly record struct` for small values. A positional `record struct` without `readonly` is mutable.
3. Validate the values of a value object in its constructor. Keep it immutable (get-only or `init`). Use `with` to make a changed copy.
4. Use `required` with `init` for types with many properties, instead of a long constructor.
5. Use a primary constructor for injected dependencies. The parameter is a captured, mutable field. Do not also copy it into a field (CS9124).
6. Use file-scoped namespaces. Put one top-level type in each file. A small closed set of nested records is one type.

### Nullability

7. Keep nullable reference types on in all code. Check input at the boundary with `ArgumentNullException.ThrowIfNull` and `ArgumentException.ThrowIfNullOrWhiteSpace`. Inside, trust the annotations and add no extra null checks.
8. Use `!` only when you can prove the value is not null, and write the proof in a comment. Use `is null` and `is not null`, because `==` can call an overloaded operator.
9. Return an empty collection, not `null`.

### Pattern matching and C# 14

10. Use switch expressions and patterns to branch on type or shape. On `net10.0` the compiler cannot see that a hierarchy has a fixed set of cases. Add a discard arm that throws.
11. Use the `field` keyword when one accessor needs logic. An existing member named `field` then needs `@field` (CS9258).
12. Use null-conditional assignment (`customer.Address?.Note = note;`). The right side runs only when the left side is not null. `++` and `--` are not allowed.
13. Use an `extension` block for extension properties and static extension members. Keep classic `this` extension methods. Do not rewrite working code for style.
14. Use collection expressions (`[]`, `[.. first, .. second]`). Use `params ReadOnlySpan<T>` for a hot path.
15. Do not use C# 15 features (`union`, `closed`, `with(...)` in a collection expression) in `net10.0` code. They do not compile.

### Async and cancellation

16. Make the call chain async from top to bottom. Do not block on a task with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`. Blocking starves the thread pool and can deadlock.
17. Return `Task`, not `async void`. Only an event handler can be `async void`, because an exception in `async void` stops the process.
18. Give each async method that does I/O a `CancellationToken cancellationToken` as the last parameter. Pass it to each call (CA2016).
19. Do not write `ConfigureAwait(false)` in app code. Use it in each `await` of a reusable library package.
20. Return `Task` by default. Use `ValueTask` only for a measured hot path that often completes synchronously. Await a `ValueTask` one time only.
21. Return `IAsyncEnumerable<T>` for a large or streamed result. Mark the token parameter with `[EnumeratorCancellation]`. On `net10.0`, the core libraries contain LINQ for async streams. Remove `System.Linq.Async`.
22. Run independent I/O with `Task.WhenAll`. Limit the number of parallel calls with `Parallel.ForEachAsync` or `SemaphoreSlim`. Use `Task.Run` only for CPU work.
23. Create a `TaskCompletionSource` with `TaskCreationOptions.RunContinuationsAsynchronously`. Dispose each `CancellationTokenSource` that you create.
24. Catch `OperationCanceledException` only to stop cleanly after a cancel of your own token. Do not log it as an error. A catch-all uses `when (ex is not OperationCanceledException)`.

### Time

25. Get the time, delays, and timers from an injected `TimeProvider`: `GetUtcNow()`, `Task.Delay(delay, time, ct)`, `new PeriodicTimer(period, time)`, `new CancellationTokenSource(timeout, time)`. Tests then control the time with `FakeTimeProvider`.
26. Store an instant as a UTC `DateTimeOffset`. Use `DateOnly` for a date without a time. Convert to local time only for display.

### Disposal

27. Dispose what you create: `using` or `await using` declarations. Do not dispose a dependency that you received from the caller or from DI.
28. Implement `IAsyncDisposable` when the cleanup does I/O. A `sealed` type needs no finalizer and no `Dispose(bool)`. Wrap an unmanaged handle in a `SafeHandle`.

### Exceptions

29. Use exceptions for failures that the caller does not expect. Return a result value for expected outcomes: not found, conflict, validation failure.
30. Use the throw helpers: `ThrowIfNull`, `ThrowIfNullOrWhiteSpace`, `ThrowIfNegative`, `ObjectDisposedException.ThrowIf` (CA1510 to CA1513).
31. Catch a specific exception type. Rethrow with `throw;` (CA2200). To add context, throw a new exception with the original one as the inner exception. Catch all exceptions only at the top boundary.

### Collections, LINQ, and strings

32. Accept `IEnumerable<T>` or `IReadOnlyList<T>`. Return `IReadOnlyList<T>` or `IReadOnlyCollection<T>`.
33. Enumerate a LINQ query one time. Call `ToList()` or `ToArray()` when you need the result again.
34. Use `TryGetValue`, not `ContainsKey` and then the indexer. Use `FrozenDictionary` or `FrozenSet` for a lookup that does not change. Give each string-keyed collection a `StringComparer`.
35. Use the current LINQ methods: `CountBy`, `AggregateBy`, `Index` (.NET 9), `LeftJoin`, `RightJoin` (.NET 10). In a measured hot path, use a loop.
36. Give each parse and format call a culture. Use `CultureInfo.InvariantCulture` for machine data and `StringComparison.Ordinal` for identifiers (CA1304, CA1305, CA1309, CA1310).
36a. When code reads a map or an enum from configuration, bind `Dictionary<string, string>`. Convert each entry yourself, with an error that names the bad key or value. The reason (tested on .NET 10): the binder gives no error in three cases. It binds no entries for `Guid` keys. It skips a key or a value that it cannot convert. It binds `"7"` to an enum. `ErrorOnUnknownConfiguration` stops only the skipped key or value. Write this reason in the Cause line of the report.

### Project files

37. Do not set `<LangVersion>`. The target framework selects the C# version. An explicit `latest` or `preview` turns on preview features.
38. Put shared properties in `Directory.Build.props` and package versions in `Directory.Packages.props`. A `PackageReference` has no `Version` (NU1008). Use exact versions, not `8.*`.
39. Add a new package only after the human approves it (stack rule 5). MediatR and AutoMapper are not allowed. Call handler classes directly, and write mappings by hand or with Mapperly.

## Never write

| Wrong (old, risky, or invented) | Write instead |
|---|---|
| `public async void SaveAsync()` | `public async Task SaveAsync(CancellationToken cancellationToken)` |
| `var x = GetAsync().Result;` | `var x = await GetAsync(cancellationToken);` |
| `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.UtcNow` in logic | `time.GetUtcNow()` with an injected `TimeProvider` |
| Own `IClock` or `ISystemClock` interface | `TimeProvider` (in the BCL since .NET 8) |
| `Thread.Sleep(1000)` | `await Task.Delay(TimeSpan.FromSeconds(1), time, cancellationToken)` |
| `new TaskCompletionSource()` | `new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)` |
| `Task.Run(() => SaveAsync())` and no await | `await SaveAsync(cancellationToken)`, or a queue that a background service reads |
| `ConfigureAwait(false)` on each line of app code | Plain `await` |
| `if (x == null) throw new ArgumentNullException("x");` | `ArgumentNullException.ThrowIfNull(x);` |
| `catch (Exception ex) { throw ex; }` | `throw;`, or a new exception with `ex` as the inner exception |
| `catch (Exception) { }` | Catch the specific type and handle it, or let it go up |
| `if (dict.ContainsKey(k)) v = dict[k];` | `dict.TryGetValue(k, out var v)` |
| `new List<int> { 1, 2 }` in new code | `[1, 2]` |
| `#nullable disable` or `= null!` to silence warnings | Fix the type. Add `?` or `required`. |
| `#pragma warning disable CA2007` with no reason | Fix the code, or add `// <reason>` and a `restore` |
| `<LangVersion>latest</LangVersion>` | No `LangVersion` |
| `<PackageReference Include="X" Version="1.2.3" />` with CPM | `<PackageReference Include="X" />` and a `PackageVersion` |
| `union Result(Ok, Error);` or `[with(comparer), ..x]` in `net10.0` | An abstract record with sealed case records |
| A classic extension method rewritten as an `extension` block "because it is new" | Keep the working method |
| `IOrderService` for each class | A concrete `sealed` class. An interface only at a seam (SOL-09). |
| MediatR `IRequestHandler`, AutoMapper `CreateMap` | A handler class that you call directly. Manual mapping or Mapperly. |
| `System.Linq.Async` on `net10.0` | The built-in `System.Linq.AsyncEnumerable` |

## Check

Run these commands on the changed project or solution:

1. To build with the analyzers, run `dotnet build <project> -warnaserror --no-incremental`. Done when the build shows `0 Warning(s)` and `0 Error(s)`.
2. To check the format, run `dotnet format <solution> --verify-no-changes`. Done when the exit code is 0.
3. To run the tests, run `dotnet test`. Done when the summary shows 0 failed.
4. To run the rules of this skill, run `python3 <this skill folder>/scripts/stackcheck.py --root <repo>`. In an existing project, add `--changed <base branch>`. Done when the summary shows `0 errors`.

The analyzer packages, `.editorconfig` severities, and `BannedSymbols.txt` belong to `dev-stack:dotnet-analyzers`.

## Review checklist

| # | Check | How |
|---|-------|-----|
| 1 | No `async void`, except event handlers | tool (CS-ASYNC-VOID) |
| 2 | No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` on tasks | tool (CS-GETRESULT, CS-TASK-BLOCK) |
| 3 | Each async I/O method takes a `CancellationToken` and passes it on | tool (CA2016), judge for new signatures |
| 4 | No `DateTime.Now`, `UtcNow`, `DateTimeOffset.UtcNow`, or `Thread.Sleep` outside tests | tool (CS-CLOCK, CS-THREAD-SLEEP) |
| 5 | Nullable is on. No `#nullable disable`. Each new `!` has a reason. | tool (CS-NULLABLE-DISABLE, CS86xx), judge for `!` |
| 6 | Each suppression has a rule ID and a reason | tool (CS-PRAGMA-NO-REASON) |
| 7 | No MediatR, no AutoMapper, no unapproved new package | tool (CS-PKG-MEDIATR, CS-PKG-AUTOMAPPER), judge |
| 8 | No `LangVersion`, no floating versions, CPM on | tool (CS-LANGVERSION, CS-FLOATING-VERSION, NU1008) |
| 9 | Classes are `sealed`, data types are records, value objects validate | judge (CA1852 for internal types) |
| 10 | Expected outcomes are result values, not exceptions. No `throw ex;`. No empty catch. | judge (CA2200) |
| 11 | The code disposes each disposable that it creates, and no injected dependency | judge (CA2000 when on) |
| 12 | No C# feature newer than the target framework allows | tool (build) |
| 13 | LINQ queries run one time. Lookups use `TryGetValue`. Strings use a culture or comparer. | judge (CA1304, CA1305, CA1309, CA1310) |
| 14 | `TaskCompletionSource` uses `RunContinuationsAsynchronously` | tool (CS-TCS-OPTIONS) |
| 15 | The change follows the accepted decisions in `docs/dev-team/decisions.md` | judge |

The full list is in `references/review-checklist.md`.

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/stack-rules.md`: read first, before each task (common rules of all stack skills).
- `references/decisions.md`: read before you create a solution, a project, a props file, or the first service class.
- `references/language-features.md`: read when you use or review a C# 12, 13, or 14 feature, or when old code breaks after an upgrade to .NET 10.
- `references/async.md`: read when you write async code, streams, timeouts, timers, or code that reads the time.
- `references/types-and-errors.md`: read when you design records, value objects, result types, disposal, exceptions, or collections.
- `references/project-conventions.md`: read when you edit `Directory.Build.props`, `Directory.Packages.props`, `global.json`, or `.editorconfig`.
- `references/net11.md`: read only when the project targets `net11.0`.
- `references/review-checklist.md`: read when you review a change.
- `references/sources.md`: read when you need the primary source of a rule or must check a fact again.
- `references/language.md`: read before you write text for a human (ASD-STE100 for English, B1 for Dutch).
