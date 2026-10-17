# FluentAssertions 7

Facts for FluentAssertions 7.2.2 (Apache-2.0) with MSTest 4.5.1. Checked on 2026-10-16. The bridge also works with FluentAssertions 6.12.2 (compiled and run). All code compiled and passed: `examples/tests/Shop.UnitTests/`.

## Contents

- License and version pin
- The MSTest 4 bridge
- Core patterns
- Equivalency traps
- APIs that do not compile in version 7
- Global options

## License and version pin

- Version 7 is the last Apache-2.0 line. It gets bug fixes only.
- Version 8 and later use the Xceed commercial license. Company use needs a paid license, and the package forces license acceptance. Do not add it, and do not accept an upgrade to it.
- Pin a range, so that a bot cannot move to version 8:

  ```xml
  <PackageReference Include="FluentAssertions" Version="[7.2.2,8.0.0)" />
  ```

- Companion packages: `FluentAssertions.Web` 3.0.0 needs FluentAssertions `[6.5.1,8.0.0)`. `FluentAssertions.Json` 8.0.0 needs FluentAssertions 8, so do not use it with version 7. Each new package needs the approval of the human (stack rule 5).
- `FluentAssertions.Analyzers` 0.34.1 (MIT) had its last release in 2024. Check that it works with your version before you suggest it.

## The MSTest 4 bridge

FluentAssertions 7 finds the test framework with an assembly scan. It looks for `Microsoft.VisualStudio.TestPlatform.TestFramework`, the MSTest 3 name. MSTest 4 renamed the assembly to `MSTest.TestFramework`. FA 8.8 added MSTest 4 detection. Version 7 did not get it.

Effect without the bridge: FA throws its own `FluentAssertions.Execution.AssertionFailedException`. MSTest reports "threw exception" with a long FA stack, not an assertion failure.

The fix is one line in the one `[AssemblyInitialize]` of the test project:

```csharp
using FluentAssertions.Common;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace Shop.UnitTests;

[TestClass]
public static class AssemblyFixture
{
    // FluentAssertions 7 does not detect MSTest 4. This line reports its failures as MSTest assertion failures.
    [AssemblyInitialize]
    public static void RouteFluentAssertionsFailures(TestContext _)
        => Services.ThrowException = message => throw new AssertFailedException(message);
}
```

- One `[AssemblyInitialize]` per assembly (a second one fails discovery with UTA013). When the project already has one, put the line at its top.
- The app.config key `FluentAssertions.TestFramework` cannot help. Version 7 knows only `mstestv2`.
- `AssertionScope` also throws through `Services.ThrowException`. One bridge covers both.
- Proof: a test that fails on purpose shows "Expected value to be 2, but found 1" and no FA frames in the stack.

## Core patterns

```csharp
// Values and graphs
result.Should().Be(10m);
order.Should().BeEquivalentTo(new Order(7, 10m, Now, ["a", "b"]));
order.Should().BeEquivalentTo(new { Id = 7, ClosedAt = Now }, options => options.ExcludingMissingMembers());

// Collections
store.Saved.Should().ContainSingle().Which.ClosedAt.Should().Be(Now);
lines.Should().Equal("a", "b");
lines.Should().HaveCountGreaterThanOrEqualTo(2);

// Exceptions
Action act = () => Discount.For(-1m);
act.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("total");

Func<Task> close = () => sut.CloseAsync(7, TestContext.CancellationToken);
await close.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("*already closed*");

// HTTP: version 7 still has HttpResponseMessage assertions
response.Should().HaveStatusCode(HttpStatusCode.OK);
```

- `Throw<T>` accepts derived types and unwraps `AggregateException`. `ThrowExactly<T>` does neither.
- `WithMessage` uses wildcards (`*`, `?`) and ignores case.
- Use `ThrowAsync` and `ThrowExactlyAsync` for async code. The sync forms do not wait for a `Task`.

## Equivalency traps

Each trap is a passing test in `examples/tests/Shop.UnitTests/EquivalencyTrapTests.cs`.

1. **`BeEquivalentTo` ignores collection order**, also in nested collections. Add `WithStrictOrdering()`, or `WithStrictOrderingFor(x => x.Lines)` for one member. Byte arrays are always strict.
2. **`BeEquivalentTo` compares a class that overrides `Equals` by value.** It compares records, record structs, anonymous types, and tuples member by member. For any other type with its own `Equals`, it calls that `Equals`. `Excluding(m => m.Note)` then has no effect, and the assertion fails. Version 7 gives no hint. Fix:

   ```csharp
   actual.Should().BeEquivalentTo(
       new Money(10m, "EUR", "note-b"),
       options => options.ComparingByMembers<Money>().Excluding(m => m.Note));
   ```

3. **The declared type wins.** When the variable is a base type, FA compares only the base members. Add `RespectingRuntimeTypes()`.
4. **`Excluding` with a member predicate.** To exclude by name, write the parameter type: `options.Excluding((IMemberInfo m) => m.Name == "Id")` (namespace `FluentAssertions.Equivalency`). Without the type, the compiler can pick the member-path overload. This happens when the class has a `Name` property. The test then throws `ArgumentException` at run time.
5. **`ComparingRecordsByValue()`** uses the record `Equals`. A record with a list member then compares list references, and equal data fails.
6. **The depth limit is 10 levels.** Deeper graphs need `AllowingInfiniteRecursion()`.

## APIs that do not compile in version 7

LLM output and current web docs often use the FluentAssertions 8 API. Each row is a compile error in 7.2.2 (verified). The last column shows if AwesomeAssertions 9.6 has the member (checked in its approved public API).

| FluentAssertions 8 | FluentAssertions 7 | In AwesomeAssertions 9.6 |
|--------------------|--------------------|--------------------------|
| `AssertionConfiguration.Current.Equivalency.Modify(...)` | `AssertionOptions.AssertEquivalencyUsing(...)` | yes |
| `EquivalencyOptions<T>` | `EquivalencyAssertionOptions<T>` | yes |
| `PreferringRuntimeMemberTypes()` | `RespectingRuntimeTypes()` | yes |
| `PreferringDeclaredMemberTypes()` | `RespectingDeclaredTypes()` | yes |
| `WithoutRecursing()` | `ExcludingNestedObjects()` | yes |
| `ExcludingMembersNamed("Id")` | `Excluding((IMemberInfo m) => m.Name == "Id")` | yes (9.3) |
| `subject.Should().Satisfy<T>(x => ...)` on an object | `BeEquivalentTo` or `Match`. `Satisfy` exists only on collections. | yes |
| `AssertionChain.GetOrCreate()` in a custom assertion | `Execute.Assertion` | yes |
| `act.Should().Throw()` (no type) | `act.Should().Throw<Exception>()` | no |
| `await act.Should().ThrowAsync()` (no type) | `await act.Should().ThrowAsync<Exception>()` | no |
| `.WithoutMessage("*x*")` | `.Where(e => !e.Message.Contains("x"))` | no |
| `WithStrictTyping()` | Assert the type with `BeOfType<T>()` | no |
| `collection.Should().BeEqualTo(...)` | `collection.Should().Equal(...)` | no |
| `ComparingNullCollectionsAsEmpty()` | Not in version 7 | no |
| JSON node assertions, `BeJsonSerializable` | Not in version 7 | no |

Old names that are still in version 7 but gone in version 8: `HaveCountGreaterOrEqualTo`, `BeGreaterOrEqualTo`, `Execute.Assertion`. Version 6 or older names: `ShouldThrow<T>()` and `Monitor(utcNow)` do not compile. Use `act.Should().Throw<T>()`.

## Global options

```csharp
[AssemblyInitialize]
public static void Initialize(TestContext _)
{
    Services.ThrowException = message => throw new AssertFailedException(message);
    AssertionOptions.AssertEquivalencyUsing(options => options.ComparingByMembers<Money>());
}
```

Global options change each test in the assembly. Set them one time in `[AssemblyInitialize]`, never in a test.
