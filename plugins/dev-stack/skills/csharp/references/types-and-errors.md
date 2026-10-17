# Types, errors, disposal, and collections

Detail for rules 1 to 10 and 27 to 36 of `SKILL.md`. All code compiles on `net10.0` with `AnalysisLevel latest-recommended` and `-warnaserror`.

## Contents

- Records and value objects
- Sealed classes
- Result types on net10.0
- Nullability
- Exceptions
- Disposal
- Collections and LINQ
- Strings and culture

## Records and value objects

| Need | Type |
|------|------|
| DTO, message, event | `sealed record` (positional is fine) |
| Value object with rules | `sealed record` with a validating constructor and get-only properties |
| Small value (two or three fields) on a hot path | `readonly record struct` |
| Entity with identity and changing state | `sealed class` |

```csharp
public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public decimal Amount { get; }

    public string Currency { get; }
}
```

- A positional record (`record Money(decimal Amount, string Currency)`) does not validate. Use it for DTOs, not for value objects with rules.
- `with` copies the object and skips the constructor. A value object that must always be valid needs get-only properties, so that `with` cannot set them.
- A positional `record struct` without `readonly` has mutable properties.
- Record equality compares all fields. A collection field compares by reference, not by content.

## Sealed classes

- Make each class `sealed` unless you design it for inheritance. The JIT can then call methods directly and inline them, and a reader knows that no subclass changes the behavior.
- CA1852 reports internal types that can be sealed. For public types, it is a judge item in review.
- Exceptions to this rule: a base class that is part of the design, and types that a framework must proxy (some EF Core lazy-loading entities).

## Result types on net10.0

C# 14 has no union types. For a closed set of outcomes, use an abstract record with a private constructor and nested sealed case records. Only the nested types can derive from it.

```csharp
public abstract record SaveResult
{
    private SaveResult()
    {
    }

    public sealed record Saved(Guid Id) : SaveResult;

    public sealed record Conflict(string Reason) : SaveResult;

    public sealed record NotFound : SaveResult;
}

public static class SaveResultText
{
    public static string Describe(SaveResult result) => result switch
    {
        SaveResult.Saved(var id) => $"saved {id}",
        SaveResult.Conflict(var reason) => $"conflict: {reason}",
        SaveResult.NotFound => "not found",
        _ => throw new ArgumentOutOfRangeException(nameof(result)),
    };
}
```

- The compiler does not know that the set is closed. Add a discard arm that throws, so that a new case fails loudly.
- Before you add a result library (for example OneOf, ErrorOr, FluentResults), ask the human (stack rule 5). The error contract of an API is decision API-05 in `dev-stack:aspnetcore`.
- On `net11.0`, use a `union` or a `closed` record instead. See `net11.md`.

## Nullability

- `<Nullable>enable</Nullable>` in `Directory.Build.props`, and nullable warnings as errors (SOL-06).
- Check input at the boundary: public methods, deserialized data, data from a database or a file. Inside, trust the annotations. A redundant null check hides the real contract.
- Annotate what can be null with `?`. Use `required` for properties that each caller must set.
- `!` says "I know it is not null". Use it only with a proof, and write the proof in a comment. `= null!` on a property is a smell: use `required`.
- Do not combine `?.` and `!` on one value. They say opposite things.
- `#nullable disable` turns off annotations and warnings. Use it only in generated code. `#nullable disable warnings` hides the warnings but keeps the annotations: it is also not allowed in written code.
- Use `[NotNullWhen(true)]`, `[MaybeNullWhen(false)]`, and `[MemberNotNull]` on `Try` methods and init helpers, so that callers get correct flow analysis.

## Exceptions

| Situation | Do |
|-----------|----|
| Bad argument from a caller | Throw helper: `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfNegative`, `ThrowIfGreaterThan` |
| Object used after `Dispose` | `ObjectDisposedException.ThrowIf(_disposed, this)` |
| Object in the wrong state | `InvalidOperationException` with the state in the message |
| Expected outcome (not found, conflict, validation) | A result value, not an exception |
| Parse user input | `TryParse`, not `Parse` in a `try` |
| Rethrow | `throw;` (keeps the stack trace, CA2200) |
| Add context | `throw new XException("context", ex);` |
| Own exception type | Only when a caller catches it. `sealed`, derive from `Exception`, message and inner exception constructors |

```csharp
public sealed class OrderImportException(string message, Exception inner) : Exception(message, inner);

public sealed class OrderImporter(IOrderStore store)
{
    public async Task ImportAsync(Order order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        try
        {
            await store.SaveAsync(order, cancellationToken);
        }
        catch (IOException ex)
        {
            throw new OrderImportException($"Import of order {order.Id} failed.", ex);
        }
    }
}
```

- Catch the most specific type that you can handle. Catch `Exception` only at the top boundary (a request, a message, a loop iteration) and log it there.
- Do not catch and ignore (`catch { }`). Do not catch to log and rethrow at each level: the top boundary logs it one time.
- Do not use exceptions for control flow. A throw costs microseconds, and in a hot path that is slow.
- Do not put secrets, tokens, or personal data in an exception message.

## Disposal

- Dispose what you create, with a `using` or `await using` declaration. Use `await using` when the type implements `IAsyncDisposable`.
- Do not dispose what you receive from a caller or from DI. The owner disposes it.
- Implement `IAsyncDisposable` when cleanup does I/O (flush, network close). Implement `IDisposable` too when synchronous callers exist.
- A `sealed` class without unmanaged resources needs no finalizer and no `Dispose(bool)` method. A simple `Dispose` is enough.
- Wrap an unmanaged handle in a `SafeHandle`. Then no other class needs a finalizer.
- A type that owns a disposable field is disposable too.

```csharp
public sealed class ReportWriter(Stream output) : IAsyncDisposable
{
    private readonly StreamWriter _writer = new(output);

    public Task WriteLineAsync(string line, CancellationToken cancellationToken) =>
        _writer.WriteLineAsync(line.AsMemory(), cancellationToken);

    public async ValueTask DisposeAsync() => await _writer.DisposeAsync();
}
```

## Collections and LINQ

| Need | Use |
|------|-----|
| Parameter that you enumerate one time | `IEnumerable<T>` |
| Parameter that you index or count | `IReadOnlyList<T>` or `IReadOnlyCollection<T>` |
| Return value | `IReadOnlyList<T>`, an array, or `IAsyncEnumerable<T>` for streams |
| Lookup that does not change after startup | `FrozenDictionary` or `FrozenSet` |
| String keys | Give a `StringComparer` (`Ordinal` or `OrdinalIgnoreCase`) |
| Empty collection | `[]` |

```csharp
public static class OrderReports
{
    private static readonly FrozenDictionary<string, string> s_currencyNames =
        new Dictionary<string, string> { ["EUR"] = "Euro", ["USD"] = "US dollar" }
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<Order> Expensive(IEnumerable<Order> orders, decimal limit) =>
        orders.Where(o => o.Total > limit).ToList();

    public static string CurrencyName(string code) =>
        s_currencyNames.TryGetValue(code, out var name) ? name : code;

    public static Dictionary<bool, int> CountByState(IEnumerable<Order> orders) =>
        orders.CountBy(o => o.ClosedAt is not null).ToDictionary();
}
```

- LINQ runs when you enumerate it. Enumerate one time, or materialize with `ToList()`.
- Use `Any()`, not `Count() > 0`. Use the `Count` or `Length` property when it exists (CA1829).
- Use `TryGetValue`, not `ContainsKey` and the indexer: one lookup, not two.
- New LINQ methods: `CountBy`, `AggregateBy`, `Index` (.NET 9). `LeftJoin`, `RightJoin` (.NET 10). `Shuffle` (.NET 10).
- In a measured hot path, a `foreach` loop is faster than LINQ and does not allocate.
- An `IQueryable` (EF Core) is not an `IEnumerable` in memory. Query rules belong to `dev-stack:efcore`.

## Strings and culture

- Machine data (files, JSON, logs, keys): `CultureInfo.InvariantCulture` and `StringComparison.Ordinal`.
- Text that a person reads: the culture of the user.
- `ToUpperInvariant()`, not `ToUpper()`. `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)`, not `a.ToLower() == b.ToLower()`.
- Analyzers CA1304, CA1305, CA1309, CA1310, CA1311 report missing culture and comparison arguments.
- Use `[GeneratedRegex]` for a fixed pattern. Give each `Regex` that reads user input a timeout.
