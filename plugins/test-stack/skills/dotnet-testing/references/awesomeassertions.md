# AwesomeAssertions 9

Facts for AwesomeAssertions 9.6.0 (Apache-2.0, published 2026-08-20) with MSTest 4.5.1. Checked on 2026-10-16 against nuget.org, the release notes, and the approved public API file of the 9.6.0 tag. All code compiled and passed: `examples/tests/Shop.UnitTests.Awesome/`.

## Contents

- What it is
- Setup
- Core patterns
- Differences from FluentAssertions 7
- Equivalency traps
- Companion packages

## What it is

- A community fork of FluentAssertions. Version 8.0.0 was a drop-in copy of FluentAssertions 8.0.0 under the old Apache-2.0 license. The maintainers state that the license will not change.
- Versions 7.x and 8.x keep the `FluentAssertions` namespace. Version 9.0 renamed all namespaces and the assembly to `AwesomeAssertions`, with no other change.
- Version 9.3 added MSTest 4 detection. It needs no bridge in `[AssemblyInitialize]`. A failure shows as an MSTest assertion failure (verified).
- It follows the FluentAssertions 8.0 API, but not the later FA 8.x additions. See the table in "Differences from FluentAssertions 7".

## Setup

```xml
<PackageReference Include="AwesomeAssertions" Version="9.6.0" />
```

```csharp
using AwesomeAssertions;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
```

Namespaces: `AwesomeAssertions`, `AwesomeAssertions.Equivalency`, `AwesomeAssertions.Execution` (for `AssertionScope`), `AwesomeAssertions.Extensions`.

Global options, set one time in `[AssemblyInitialize]`:

```csharp
[AssemblyInitialize]
public static void Initialize(TestContext _)
    => AssertionConfiguration.Current.Equivalency.Modify(options => options.ComparingByMembers<Money>());
```

## Core patterns

```csharp
order.Should().BeEquivalentTo(new Order(7, 10m, Now, ["a", "b"]));
subject.Should().BeEquivalentTo(expected, options => options.ExcludingMembersNamed("Id").WithStrictOrdering());
animal.Should().BeEquivalentTo(new Dog { Name = "Rex", Legs = 4 }, options => options.PreferringRuntimeMemberTypes());

dog.Should().Satisfy<Dog>(d =>
{
    d.Name.Should().Be("Rex");
    d.Legs.Should().Be(4);
});

Action act = () => Discount.For(-1m);
act.Should().ThrowExactly<ArgumentOutOfRangeException>().WithParameterName("total");

Func<Task> close = () => sut.CloseAsync(7, TestContext.CancellationToken);
await close.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("*already closed*");

response.StatusCode.Should().Be(HttpStatusCode.OK);
```

## Differences from FluentAssertions 7

Code for FluentAssertions 7 needs these changes. Each row was a compile error after a pure namespace change (verified).

| FluentAssertions 7 | AwesomeAssertions 9 |
|--------------------|---------------------|
| `using FluentAssertions;` | `using AwesomeAssertions;` |
| `Services.ThrowException = ...` (MSTest 4 bridge) | Remove it. Version 9.3 and later detect MSTest 4. |
| `AssertionOptions.AssertEquivalencyUsing(...)` | `AssertionConfiguration.Current.Equivalency.Modify(...)` |
| `EquivalencyAssertionOptions<T>` | `EquivalencyOptions<T>` |
| `RespectingRuntimeTypes()` | `PreferringRuntimeMemberTypes()` |
| `RespectingDeclaredTypes()` | `PreferringDeclaredMemberTypes()` |
| `ExcludingNestedObjects()` | `WithoutRecursing()` |
| `Excluding((IMemberInfo m) => m.Name == "Id")` | Still compiles. `ExcludingMembersNamed("Id")` is shorter. |
| `HaveCountGreaterOrEqualTo`, `BeGreaterOrEqualTo` | `HaveCountGreaterThanOrEqualTo`, `BeGreaterThanOrEqualTo` |
| `response.Should().HaveStatusCode(...)` | `response.StatusCode.Should().Be(...)`, or `AwesomeAssertions.Web` (new package, human approval) |
| `Execute.Assertion` in a custom assertion | `AssertionChain.GetOrCreate()` |
| `AllSatisfy` and `OnlyContain` fail on an empty collection | They pass on an empty collection. Add `NotBeEmpty()` when empty is a bug. |

FluentAssertions 8 members that AwesomeAssertions 9.6 does not have. LLM output from FA 8 docs often uses them:

| Not in AwesomeAssertions 9.6 | Use |
|------------------------------|-----|
| `act.Should().Throw()` (no type) | `act.Should().Throw<Exception>()` |
| `.WithoutMessage("*x*")` | `.And.Message.Should().NotContain("x")` |
| `WithStrictTyping()` | `BeOfType<T>()` as a separate assertion |
| `collection.Should().BeEqualTo(...)` | `collection.Should().Equal(...)` |
| `ComparingNullCollectionsAsEmpty()` | Make both sides empty in the test data |

## Equivalency traps

- `BeEquivalentTo` ignores collection order, as in version 7. Add `WithStrictOrdering()`.
- `BeEquivalentTo` compares a class that overrides `Equals` by value. `Excluding(...)` on it has no effect and the assertion fails. Add `ComparingByMembers<T>()` (verified in `EquivalencyTrapTests.cs`).
- The declared type wins. Add `PreferringRuntimeMemberTypes()`.

## Companion packages

Each needs the approval of the human (stack rule 5).

| Package | Version | License | For |
|---------|---------|---------|-----|
| `AwesomeAssertions.Web` | 3.0.0 | Apache-2.0 | `HttpResponseMessage` assertions (`Be200Ok()`, `HaveHttpStatusCode`) |
| `AwesomeAssertions.Json` | 9.0.0 | Apache-2.0 | Newtonsoft.Json `JToken` assertions |
| `AwesomeAssertions.DataSets` | 9.0.0 | Apache-2.0 | `DataSet` and `DataTable` assertions |
| `AwesomeAssertions.Analyzers` | 9.0.8 | MIT | Finds wrong assertion use at build time |
