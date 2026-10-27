# .NET 11 and C# 15

Read this file only when the project targets `net11.0`. On `net10.0`, none of these features compile.

Status on 2026-10-27: .NET 11 is at RC 3 (11.0.0-rc.3 of 2026-10-25, SDK 11.0.100-rc.3.26522.148) with go-live support. GA is expected in November 2026. .NET 11 is an STS release (24 months of support). C# 15 is the default language version for `net11.0`. The examples in this file compiled with SDK 11.0.100-rc.2. Check the features again after GA (stack rule 2).

## Contents

- Before you use .NET 11
- Union types
- Closed hierarchies
- Collection expression arguments
- Extension indexers
- Labeled break and continue
- Runtime async
- New keywords that break old code
- Library and behavior changes
- Not for app code

## Before you use .NET 11

- .NET 10 is LTS (support until 2028-11-14). .NET 11 is STS. Moving a project to `net11.0` is a decision: ask the human (SOL-03 "Revisit when").
- Update `global.json` (SOL-05) and `TargetFramework` together. Do not set `LangVersion`.
- A library that also targets `net10.0` cannot use C# 15 features. Write for the lowest target.
- On the .NET 11 SDK, the environment variable `DOTNET_TEST_RUNNER` (`VSTest` or `Microsoft.Testing.Platform`) overrides the `test` runner of `global.json`. Keep the runner in `global.json`, so that each machine and CI use the same runner.

## Union types

A union holds one value of a fixed set of case types. The compiler checks that a `switch` handles each case. Use it for result-or-error returns and closed sets of messages, instead of a marker interface or the abstract-record pattern of `types-and-errors.md`.

```csharp
public sealed record Saved(Guid Id);

public sealed record Conflict(string Reason);

public sealed record NotFound;

public union SaveResult(Saved, Conflict, NotFound);

public static class Describe
{
    public static string Result(SaveResult result) => result switch
    {
        Saved s => $"saved {s.Id}",
        Conflict c => $"conflict: {c.Reason}",
        NotFound => "not found",
    };

    public static SaveResult Save(bool exists) => exists ? new Saved(Guid.NewGuid()) : new NotFound();
}
```

- Each case type converts implicitly to the union. Return the case value directly.
- Patterns apply to the `Value` inside the union. `result is SaveResult` does not test the union itself.
- The generated union is a `struct` that stores the value as `object?`. A value-type case is boxed. `default(SaveResult)` has a null `Value`: handle `null` when the value can be the default.
- A union adds no equality, no `with`, and no deconstruction. Put data in the case records.
- Do not add a result library (OneOf, ErrorOr) to a `net11.0` project for new code. Ask the human first when the project already has one.
- System.Text.Json in .NET 11 supports C# unions.

## Closed hierarchies

A `closed` class or record can only have direct subclasses in its own assembly. A `switch` over its direct subclasses is exhaustive without a discard arm.

```csharp
public closed record GateState;

public sealed record GateClosed : GateState;

public sealed record GateOpen(float Percent) : GateState;

public static string Gate(GateState state) => state switch
{
    GateClosed => "closed",
    GateOpen(var percent) => $"{percent}% open",
};
```

- A `closed` type is implicitly abstract. Do not combine it with `sealed`, `static`, or `abstract`.
- Use `closed` when the cases share data or behavior. Use a `union` when the cases are unrelated types.
- Mark the case types `sealed`, or mark a middle level `closed` too. A non-closed subclass can get subclasses in other assemblies.

## Collection expression arguments

`with(...)` as the first element passes arguments to the constructor of the collection.

```csharp
public static HashSet<string> Codes(IEnumerable<string> codes) =>
    [with(StringComparer.OrdinalIgnoreCase), .. codes];

public static List<int> WithCapacity(int[] values) => [with(capacity: values.Length * 2), .. values];
```

- Use it for a comparer or a capacity. It replaces `new HashSet<string>(comparer) { ... }`.
- Breaking change: in C# 14, `[with(x, y)]` called a method named `with`. In C# 15 it is constructor arguments. Write `@with(x, y)` to call the method.

## Extension indexers

An `extension` block can declare an indexer: `extension(IEnumerable<int> sequence) { public int this[int index] => sequence.ElementAt(index); }`. Use it only when the index is cheap. An indexer that enumerates a sequence hides an O(n) cost.

## Labeled break and continue

```csharp
outer: for (var row = 0; row < cells.GetLength(0); row++)
{
    for (var column = 0; column < cells.GetLength(1); column++)
    {
        if (cells[row, column] == target)
        {
            found = (row, column);
            break outer;
        }
    }
}
```

Use it instead of a Boolean flag or `goto` in nested loops. IDE0410 suggests it. Often a separate method with `return` is clearer.

## Runtime async

- The runtime, not the compiler, can run `async` methods in .NET 11. Stack traces are shorter and `await` costs less.
- The .NET libraries and the ASP.NET Core shared framework are compiled with runtime async. Your code benefits without changes.
- For your own assemblies it is a preview opt-in: `<Features>runtime-async=on</Features>`. Do not turn it on without a decision. To turn it off, use `<UseRuntimeAsync>false</UseRuntimeAsync>`.
- The C# rules in `async.md` do not change.

## New keywords that break old code

| Keyword | Breaks | Fix |
|---------|--------|-----|
| `union` | A field or type named `union` | `@union` |
| `closed` | A type named `closed`, or a field of that type | `@closed` |
| `with` in a collection expression | A call to a method named `with` | `@with(...)` |

## Library and behavior changes

Changes that can affect C# library code (from the .NET 11 breaking changes list):

- `DateOnly.TryParse`, `TryParseExact`, and the `TimeOnly` versions throw `ArgumentException` for an invalid `DateTimeStyles` value or format. Before, they returned `false`.
- `Math.Round` and `MathF.Round` with digits give correctly rounded results. Results can differ in the last digit.
- `decimal` and `BigInteger` conversions from floating point are correctly rounded.
- Floating-point conversions to small integer types (`byte`, `short`) saturate.
- `ChangeToken.OnChange` has async overloads. An async lambda that compiled as `async void` now binds to the `Task` overload. This fixes a bug, but the timing changes.
- New APIs that replace own code: `EqualityComparer<T>.Create(keySelector)`, `FullJoin` and tuple-returning `Join` in LINQ, Zstandard in `System.IO.Compression`, `Decimal32`, `Decimal64`, `Decimal128`.
- Host changes (`IHost.RunAsync` throws when a `BackgroundService` fails, async options validation): `dev-stack:aspnetcore`.

## Not for app code

- Non-virtual static interface members on older runtimes are a C# 15 feature for libraries that target an older runtime with a newer `LangVersion`. This skill does not set `LangVersion`, so they do not apply.
- Memory safety changes (`unsafe` expressions, the `safe` keyword, pointer types without `unsafe`) need `LangVersion preview`. Do not use them.
- The Roslyn breaking changes list already names C# 16 changes. They do not apply to `net11.0`.
