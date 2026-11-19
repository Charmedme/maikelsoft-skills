# API changes from FluentAssertions to AwesomeAssertions 9

Each change that a test project meets when it moves from FluentAssertions 5, 6, or 7 to AwesomeAssertions 9.6.0. Checked on 2026-10-16 against the AwesomeAssertions release notes, the FluentAssertions upgrade guides, and the approved public API of AwesomeAssertions 9.6.0. Rows marked "verified" were compile errors in a test build after a pure namespace change.

## Contents

- How to use this file
- Namespaces and setup
- Equivalency options
- Exceptions
- Collections and numbers
- HTTP, JSON, and DataSet
- Custom assertions and extensions
- Behavior changes without a compile error
- From FluentAssertions 5
- Members that AwesomeAssertions 9.6 does not have

## How to use this file

Fix the compiler errors first. Each error points at one row. Then read "Behavior changes without a compile error": these rows can turn a passing test into a failing test, or the other way around.

## Namespaces and setup

| FluentAssertions | AwesomeAssertions 9 | Note |
|------------------|---------------------|------|
| `using FluentAssertions;` | `using AwesomeAssertions;` | verified. Version 9.0 renamed each namespace and the assembly. |
| `using FluentAssertions.Execution;` | `using AwesomeAssertions.Execution;` | `AssertionScope` |
| `using FluentAssertions.Equivalency;` | `using AwesomeAssertions.Equivalency;` | `IMemberInfo`, `EquivalencyOptions<T>` |
| `using FluentAssertions.Extensions;` | `using AwesomeAssertions.Extensions;` | `5.Minutes()`, `16.October(2026)` |
| `using FluentAssertions.Common;` + `Services.ThrowException = ...` | Remove both. | verified. 9.3 and later detect MSTest 4. |
| `<Using Include="FluentAssertions" />` | `<Using Include="AwesomeAssertions" />` | MSBuild global using |
| `global using FluentAssertions = AwesomeAssertions;` | Not possible. | A `using` alias cannot name a namespace in another `using` directive (verified, CS0246). |
| `app.config` key `FluentAssertions.TestFramework` | `AssertionConfiguration.Current.TestFramework` or `[assembly: AssertionEngineInitializer(...)]` | Version 8 removed app.config settings. Normally not needed. |

## Equivalency options

| FluentAssertions 7 | AwesomeAssertions 9 | Note |
|--------------------|---------------------|------|
| `AssertionOptions.AssertEquivalencyUsing(o => ...)` | `AssertionConfiguration.Current.Equivalency.Modify(o => ...)` | verified |
| `EquivalencyAssertionOptions<T>` | `EquivalencyOptions<T>` | verified |
| `EquivalencyAssertionOptions` | `EquivalencyOptions` | |
| `IEquivalencyAssertionOptions` | `IEquivalencyOptions` | |
| `SelfReferenceEquivalencyAssertionOptions<T>` | `SelfReferenceEquivalencyOptions<T>` | |
| `RespectingRuntimeTypes()` | `PreferringRuntimeMemberTypes()` | verified |
| `RespectingDeclaredTypes()` | `PreferringDeclaredMemberTypes()` | verified |
| `ExcludingNestedObjects()` | `WithoutRecursing()` | verified |
| `Excluding((IMemberInfo m) => m.Name == "Id")` | Same code compiles. `ExcludingMembersNamed("Id")` is shorter (9.3). | |
| `Excluding(x => x.Id)`, `Including(...)`, `WithStrictOrdering()`, `ComparingByMembers<T>()`, `ExcludingMissingMembers()` | Same | |

## Exceptions

| FluentAssertions | AwesomeAssertions 9 | Note |
|------------------|---------------------|------|
| `act.Should().Throw<T>()`, `ThrowExactly<T>()`, `NotThrow()` | Same | |
| `await act.Should().ThrowAsync<T>()`, `ThrowExactlyAsync<T>()` | Same | |
| `.WithMessage("*x*")`, `.WithInnerException<T>()`, `.WithParameterName("x")`, `.Where(...)` | Same | `WithMessage` uses wildcards and ignores case, as before. |

## Collections and numbers

| FluentAssertions 7 | AwesomeAssertions 9 | Note |
|--------------------|---------------------|------|
| `HaveCountGreaterOrEqualTo(n)` | `HaveCountGreaterThanOrEqualTo(n)` | verified |
| `HaveCountLessOrEqualTo(n)` | `HaveCountLessThanOrEqualTo(n)` | |
| `BeGreaterOrEqualTo(n)` | `BeGreaterThanOrEqualTo(n)` | verified |
| `BeLessOrEqualTo(n)` | `BeLessThanOrEqualTo(n)` | |
| `Equal(...)`, `BeEquivalentTo(...)`, `ContainSingle()`, `AllSatisfy(...)`, `SatisfyRespectively(...)` | Same | See the empty collection row in "Behavior changes". |

## HTTP, JSON, and DataSet

| FluentAssertions 7 | AwesomeAssertions 9 | Note |
|--------------------|---------------------|------|
| `response.Should().HaveStatusCode(HttpStatusCode.OK)` | `response.StatusCode.Should().Be(HttpStatusCode.OK)` | verified. Version 8 removed the `HttpResponseMessage` assertions. |
| `response.Should().BeSuccessful()` | `response.IsSuccessStatusCode.Should().BeTrue()` | Or the package `AwesomeAssertions.Web` 3.0.0 (Apache-2.0, new package: human approval). |
| Package `FluentAssertions.Web` | Package `AwesomeAssertions.Web` 3.0.0 | `FluentAssertions.Web` 3.0.0 needs FluentAssertions below 8. |
| Package `FluentAssertions.Json` | Package `AwesomeAssertions.Json` 9.0.0 | Newtonsoft.Json assertions |
| `DataSet`, `DataTable`, `DataRow` assertions | Package `AwesomeAssertions.DataSets` 9.0.0 | Version 8 moved them out of the main package. |
| Package `FluentAssertions.Analyzers` | Package `AwesomeAssertions.Analyzers` 9.0.8 (MIT) | Optional |

## Custom assertions and extensions

| FluentAssertions 7 | AwesomeAssertions 9 |
|--------------------|---------------------|
| `Execute.Assertion.ForCondition(...).FailWith(...)` | Pass an `AssertionChain` to the assertions class: `new MyAssertions(subject, AssertionChain.GetOrCreate())`, then `assertionChain.ForCondition(...).FailWith(...)`. |
| `.WithExpectation(...)` followed by `.ClearExpectation()` | `.WithExpectation(message, args, chain => chain...)`, with the nested lambda. `ClearExpectation` is gone. |
| `IEquivalencyStep.Handle(...)` with `EquivalencyAssertionOptions` | Same shape, with the renamed option types. |
| `[CustomAssertion]` on each method | Same. `[CustomAssertions]` on a class (8.2) and `[CustomAssertionsAssembly]` on the assembly also work. |

Rewrite custom assertion classes by hand, one at a time. Each rewrite needs its own test run.

## Behavior changes without a compile error

| Change | Effect on a test | What to do |
|--------|------------------|------------|
| `AllSatisfy` and `OnlyContain` pass on an empty collection (FA 8 change) | A test that relied on a failure for an empty collection now passes. | Add `NotBeEmpty()` where an empty collection is a bug. |
| `BeLowerCased` and `BeUpperCased` follow `ToLower` and `ToUpper` | A string with digits or symbols can give another result. | Check each use. |
| `XElement.Should().HaveAttribute(name, value)` now checks only the name. The value binds to the `because` text. | The test passes with a wrong value (verified). No compile error. | Rename to `HaveAttributeWithValue(name, value)`. Rule MFA-XML-HAVEATTRIBUTE lists each call. |
| String failure messages show a diff | Tests that assert on the failure message text of FA can fail. | Assert on the wildcard part of the message only. |
| Caller identification uses chained names (`element/child`) | Same as above. | Same as above. |
| `ThrowAsync` returns `ExceptionAssertionsTask<T>` (main branch, not in 9.6.0) | Only code that stores the result as `Task<ExceptionAssertions<T>>`. | Use `await` directly on the chain. |

The value-type trap stays: for a class that overrides `Equals`, `BeEquivalentTo` calls `Equals`, and `Excluding(...)` on it has no effect. Keep `ComparingByMembers<T>()` (verified in AwesomeAssertions 9.6.0).

## From FluentAssertions 5

Projects on FluentAssertions 5 also meet the version 6 breaks. Each row was a compile error with FluentAssertions 6.12.2 (verified):

| FluentAssertions 5 | Version 6 and later |
|--------------------|---------------------|
| `asyncAct.Should().Throw<T>()` on a `Func<Task>` | `await asyncAct.Should().ThrowAsync<T>()` |
| `MyEnum.One.Should().Be(OtherEnum.One)` | `HaveSameValueAs(OtherEnum.One)` or `HaveSameNameAs(...)` |
| `nonGenericEnumerable.Should().HaveCount(1)` on `IEnumerable` | `nonGenericEnumerable.Cast<object>().Should().HaveCount(1)` |
| `collection.Should().BeEquivalentTo(1, 2)` (params) | `collection.Should().BeEquivalentTo(new[] { 1, 2 })` |
| `ShouldThrow<T>()`, `ShouldNotThrow()` (version 4 style) | `act.Should().Throw<T>()` |

An async test method that calls the sync `Throw` blocks. After the change, make the test method `async Task`.

## Members that AwesomeAssertions 9.6 does not have

FluentAssertions 8.x added these after the fork. LLM output from FA 8 docs uses them. They do not compile with AwesomeAssertions 9.6.0 (checked in its public API):

| FluentAssertions 8.x | AwesomeAssertions 9.6 |
|----------------------|-----------------------|
| `act.Should().Throw()` (no type) | `act.Should().Throw<Exception>()` |
| `.WithoutMessage("*x*")` | `.And.Message.Should().NotContain("x")` |
| `WithStrictTyping()` | `BeOfType<T>()` as a separate assertion |
| `collection.Should().BeEqualTo(...)` | `collection.Should().Equal(...)` |
| `ComparingNullCollectionsAsEmpty()` | Make both sides empty in the test data |
| JSON node assertions, `BeJsonSerializable` | `AwesomeAssertions.Json` for Newtonsoft, or assert the parsed values |
