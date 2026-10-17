# C# language features

The features of C# 12, 13, and 14, the version that each one needs, how to use it, and its traps. All code compiles with SDK 10.0.402 on `net10.0`.

## Contents

- Which version has which feature
- C# 14 features
- C# 14 breaking changes
- C# 12 and 13 features that agents get wrong
- Adopt in steps

## Which version has which feature

| Feature | C# | Default for |
|---------|----|-------------|
| Primary constructors on classes, collection expressions, `ref readonly` parameters, alias any type | 12 | `net8.0` |
| `params` collections (`params ReadOnlySpan<T>`, `params IEnumerable<T>`), `System.Threading.Lock`, `\e` escape, partial properties and indexers, `field` (preview only) | 13 | `net9.0` |
| `extension` blocks, `field`, null-conditional assignment, `nameof(List<>)`, span conversions, lambda parameter modifiers, partial constructors and events, compound assignment operators | 14 | `net10.0` |
| `union`, `closed`, `with(...)` in collection expressions, extension indexers, labeled `break`/`continue` | 15 | `net11.0`, see `net11.md` |

`params` collections came in C# 13, not in C# 14. Collection expression arguments (`with(...)`) came in C# 15, not in C# 14.

## C# 14 features

### The field keyword

Use `field` when one accessor needs logic. You do not need a manual backing field.

```csharp
public sealed class Customer(Guid id, string email)
{
    public Guid Id { get; } = id;

    public string Email
    {
        get;
        set => field = value.Trim().ToLowerInvariant();
    } = email.Trim().ToLowerInvariant();
}
```

Trap: in a type that already has a member named `field`, the keyword now binds to the synthesized backing field. The compiler gives CS9258. Write `@field` or `this.field` for the old member, or rename it.

### Null-conditional assignment

```csharp
public static void SetNote(Customer customer, string note) => customer.Address?.Note = note;
```

- The right side runs only when the left side is not null. A method call on the right side does not run when `Address` is null.
- Compound assignment works: `counter?.Value += 1;`.
- `++` and `--` are not allowed.
- Do not use it to hide a bug. When `Address` must exist, a null is an error, not a case to skip.

### Extension blocks

```csharp
public static class StringExtensions
{
    extension(string value)
    {
        public bool IsBlank => string.IsNullOrWhiteSpace(value);
    }

    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
```

- Use a block for extension properties, static extension members, and extension operators.
- Classic `this` methods stay valid and are binary compatible. Do not rewrite them.
- Do not declare a classic method and a block member with the same signature in one class. The compiler reports a duplicate.
- `extension` is now a contextual keyword. A type named `extension` needs `@extension`.

### Implicit span conversions

Arrays convert to `Span<T>` and `ReadOnlySpan<T>` in more places. Methods with span parameters now bind as extension methods on arrays.

Trap: an expression tree that calls `array.Contains(x)` now binds to `MemoryExtensions.Contains`. `Compile(preferInterpretation: true)` then fails at run time. Cast to `IEnumerable<T>` or call `Enumerable.Contains(array, x)`. Some test assertions also become ambiguous. Add a cast.

### Other C# 14 features

- `nameof(List<>)` gives `"List"`. Use it in log messages and exceptions.
- `(text, out result) => int.TryParse(text, out result)`: modifiers on lambda parameters without types.
- Partial constructors and partial events: for source generators. Do not use them in hand-written code.
- User-defined compound assignment (`public void operator +=(int value)`) and `++`/`--`: only for mutable value types where an in-place update saves allocations. Do not add them to records.

## C# 14 breaking changes

These changes can break code that compiled with C# 13:

| Change | Effect | Fix |
|--------|--------|-----|
| `field` is a keyword in accessors | A member named `field` is not used any more (CS9258) | `@field`, or rename |
| `extension` is a contextual keyword | A type named `extension` does not compile | `@extension` |
| Span overload resolution | Expression trees fail with interpretation, some calls become ambiguous | Cast to the non-span type |
| `partial` is accepted on more declarations | A method `partial F()` returning type `partial` changes meaning | `@partial` |

## C# 12 and 13 features that agents get wrong

### Primary constructors

```csharp
public sealed class OrderService(IOrderStore store, TimeProvider time)
{
    public bool IsExpired(Order order, TimeSpan maxAge) => time.GetUtcNow() - order.CreatedAt > maxAge;
}
```

- The parameter is a captured field. It is mutable, not `readonly`.
- Do not copy the parameter into a field and also use the parameter (CS9124). Use one or the other.
- When the type must protect the value against change, assign it to a `private readonly` field and use only the field.
- A positional `record` gives public properties. A primary constructor on a class gives none.

### Collection expressions and params collections

```csharp
public static int[] Merge(int[] first, int[] second) => [.. first, .. second, 0];

public static int Sum(params ReadOnlySpan<int> values)
{
    var sum = 0;
    foreach (var value in values)
    {
        sum += value;
    }

    return sum;
}
```

- `[]` is an empty collection of the target type. It does not allocate for arrays and spans.
- `params ReadOnlySpan<T>` (C# 13) avoids an array allocation on each call.
- `var x = [1, 2];` does not compile: a collection expression needs a target type.

### System.Threading.Lock

Use `private readonly Lock _gate = new();` and `lock (_gate)` for new code on `net9.0` or later. The compiler uses the faster `Lock.EnterScope()`. Do not `await` inside a `lock`. Use `SemaphoreSlim` for async code.

## Adopt in steps

1. New code: `field`, null-conditional assignment, collection expressions, primary constructors for dependencies. These have no risk.
2. Only where they make a domain model clearer: extension properties, compound operators.
3. Existing code: change only the lines that the task touches. Do not modernize a whole file for style.
