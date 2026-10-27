---
name: dotnet-testing
description: .NET test code and test project setup with MSTest 4 (MSTest.Sdk, Microsoft.Testing.Platform), FluentAssertions 7.2.2 or AwesomeAssertions 9, NSubstitute, hand-written fakes, FakeTimeProvider, WebApplicationFactory, Testcontainers 4, and Respawn. Covers what to test, test names, data rows, equivalency traps, the FluentAssertions 7 bridge for MSTest 4, integration test fixtures, parallel runs, coverage, and mutation testing. Use when writing, reviewing, or fixing .NET tests, setting up a test project or global.json for tests, choosing which tests a C# class or API needs, or when a test fails with "threw exception FluentAssertions.Execution.AssertionFailedException". Not for xUnit or NUnit API details, Angular or TypeScript tests, Playwright end-to-end tests, load tests, or the move from FluentAssertions to AwesomeAssertions (dev-stack:migrate-fluentassertions).
metadata:
  verified: "2026-10-27"
  versions: ".NET 10 (SDK 10.0.403), MSTest 4.5.2, Microsoft.Testing.Platform 2.5.2, FluentAssertions 7.2.2, AwesomeAssertions 9.6.0, NSubstitute 6.2.0, Microsoft.Extensions.TimeProvider.Testing 10.11.0, Microsoft.AspNetCore.Mvc.Testing 10.0.14, Testcontainers 4.16.0, Respawn 7.0.0, Stryker.NET 5.0.0"
---

# .NET testing

Rules for .NET tests: the test project, the test design, the assertions, the test doubles, and the integration tests. The move from FluentAssertions to AwesomeAssertions: `dev-stack:migrate-fluentassertions`. The C# rules for the code under test: `dev-stack:csharp`. EF Core data access: `dev-stack:efcore`.

## First

1. Read `references/stack-rules.md`. It has the common rules of all stack skills.
2. Read the test stack of the project. Done when you can fill in each line of this table:

   | Item | Where to look | Read |
   |------|---------------|------|
   | MSTest version | `global.json` `msbuild-sdks`, `MSTest*` package in `*.csproj` or `Directory.Packages.props` | 4.x: this file and `references/mstest-4.md`. 3.x: tell the human (only the latest MSTest gets support). Do not upgrade unless the task asks. |
   | Runner | `global.json` `test.runner`, `UseVSTest`, `EnableMSTestRunner` | `references/mstest-4.md` |
   | Assertion library | `FluentAssertions` or `AwesomeAssertions` package, `using` lines | FA 7: `references/fluentassertions-7.md`. AwesomeAssertions: `references/awesomeassertions.md`. FA 8 or later: stop, see rule 6. |
   | Test doubles | `NSubstitute`, `Moq`, `FakeItEasy`, fakes in the test project | rule 15 |
   | Containers | `Testcontainers.*` packages | `references/testcontainers.md` |

3. Use the library that the project uses. Do not add a second assertion library or a second mock library. One style per repository keeps the tests easy to read.

## Defaults for new code

Existing code keeps its style (stack rule 3). For new code, these are the recommendations. Ask the human when no accepted decision exists (stack rule 4).

| Area | Default | Decision |
|------|---------|----------|
| Framework | MSTest 4 with `MSTest.Sdk`, version pinned in `global.json` | TEST-01 |
| Layout | `src/` and `tests/`, one `X.UnitTests` per project with logic, one `X.IntegrationTests` per host | TEST-04 |
| Assertions | AwesomeAssertions 9 in a new repo. FluentAssertions `[7.2.2,8.0.0)` where FA exists. | TEST-02 |
| Names | `Method_Scenario_ExpectedResult`, class `<ClassUnderTest>Tests` | TEST-05 |
| Parallel runs | Unit tests `MethodLevel`. Integration tests `ClassLevel` plus `[DoNotParallelize]` on database classes. | TEST-17 |
| Test doubles | Hand-written fakes and `FakeTimeProvider` first, NSubstitute 6 at boundaries | TEST-03 |
| API tests | `WebApplicationFactory<Program>` plus Testcontainers, one container per test assembly | TEST-07 |
| Test database | Production engine in a container, pinned tag, Respawn reset before each test | DATA-20, TEST-08 |
| Coverage | `dotnet test --coverage`, report in the PR, no gate | TEST-11 |
| CI | Unit and integration tests on each PR, no automatic retry | TEST-18 |
| Test data | Inline `new`, a builder when the same setup appears twice | TEST-06 |
| Other | No snapshot library (TEST-09), project references for layers (TEST-10), oasdiff for API contracts (TEST-13), Stryker.NET on demand (TEST-12) | |

## Rules

### Test project

1. Use `<Project Sdk="MSTest.Sdk">` and pin the version once in `global.json` under `msbuild-sdks`. The SDK adds the framework, the adapter, the analyzers, the runner, and code coverage. Dependabot and the NuGet UI do not update SDK versions, so bump it by hand.
2. Select the runner in `global.json`: `"test": { "runner": "Microsoft.Testing.Platform" }`. On the .NET 10 SDK, MSTest.Sdk projects (MTP v2 by default) fail on the old VSTest path of `dotnet test`. When `global.json` selects MTP, each test project in the repository must use MTP.
3. Set `<MSTestAnalysisMode>Recommended</MSTestAnalysisMode>` and build with warnings as errors in CI. The analyzers find removed APIs, wrong argument order, and missing cancellation.
4. Declare parallel runs in each test assembly: `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`. MSTest runs tests one by one when you declare nothing (MSTEST0001).
5. Keep unit tests and integration tests in separate projects. Then the unit test run needs no Docker and stays fast.

### Assertion library

6. Do not add or upgrade to FluentAssertions 8 or later. Version 8 needs a paid Xceed license for commercial use. Pin `[7.2.2,8.0.0)`. To move to AwesomeAssertions, use `dev-stack:migrate-fluentassertions` after the human approves.
7. With FluentAssertions 6 or 7 and MSTest 4, set the bridge in the one `[AssemblyInitialize]` of each test project. Without it, a failure shows as "threw exception: FluentAssertions.Execution.AssertionFailedException", because FA 7 looks for the MSTest 3 assembly name.

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

   An assembly can have only one `[AssemblyInitialize]`. When one exists (for example the container start), put the line at its top. AwesomeAssertions 9.3 and later detect MSTest 4 and need no bridge.
8. Write the API of the installed version. FA 7 and AwesomeAssertions 9 have different names for the same options. The tables are in `references/fluentassertions-7.md` and `references/awesomeassertions.md`.

### What to test

9. Test the critical core first: business rules, branches, boundaries, error paths, and the contract of each endpoint. A test earns its place when a real bug can make it fail.
10. Do not test framework behavior (model binding, the DI container, EF change tracking). Do not test auto-properties, generated code, log calls, or private methods. These tests break on each refactor and find no bugs.
11. Test public behavior and the outcome (return value, stored state, published message). Do not test the call order inside the class. More detail: `references/test-design.md`.

### Test shape

12. Test one behavior in each test. Name it `Method_Scenario_ExpectedResult`, for example `CloseAsync_ClosedOrder_ThrowsInvalidOperation`. The name tells the reader what broke.
13. Write Arrange, Act, Assert with one blank line between the parts. Use one Act line. Put no `if`, loops, or string building in a test. A bug in test logic hides a bug in the code.
14. Use `[DataRow]` for cases of one behavior with simple values. Use `[DynamicData]` with a property that returns tuples or `TestDataRow<T>` for other values. Give each row a `DisplayName` when the values do not explain the case.
15. Prefer real objects and hand-written fakes. Use NSubstitute only at a boundary (clock, network, message bus, payment provider). Check a call with `Received()` only when the call is the behavior, for example "the email is sent".
16. Inject `TimeProvider` in the code under test and use `FakeTimeProvider` in the test. Call `Advance` to move time. Do not use `Thread.Sleep` or `Task.Delay` to wait.
17. Use a constructor and `readonly` fields for sync setup. Use `[TestInitialize]` only for async setup. Make test classes `sealed`.
18. Flow `TestContext.CancellationToken` into each async call (MSTEST0049). Set `CooperativeCancellation = true` on each `[Timeout]` (MSTEST0045).
19. Use the smallest data that proves the behavior. Give magic values a name (`const`, `static readonly`).

### Assertions

20. Use `BeEquivalentTo` for records and DTOs, and `Be` for primitives and value objects.
21. `BeEquivalentTo` ignores collection order. Add `WithStrictOrdering()` when the order is part of the contract.
22. For a class that overrides `Equals`, `BeEquivalentTo` calls `Equals`. It does not compare member by member. `Excluding(...)` on such a class has no effect, and the assertion fails. Add `ComparingByMembers<T>()`.
23. Assert an exception on a delegate: `Action act = () => sut.Run();` then `act.Should().ThrowExactly<T>()`. For async code, use `Func<Task>` and `await act.Should().ThrowExactlyAsync<T>()`. `Throw<T>` also accepts derived types.
24. `WithMessage` uses wildcards (`*`, `?`), not a regex, and ignores case.
25. Use `AssertionScope` only when several facts describe one outcome. It is not a way to test many behaviors in one test.

### Integration tests

26. Start one container per test assembly in `[AssemblyInitialize]` and dispose it in `[AssemblyCleanup]`. A container start costs seconds. MSTest has no class fixture like xUnit.
27. Give each builder a pinned image: `new PostgreSqlBuilder("postgres:18.6-alpine")`. Use the tag that production uses. Testcontainers 4.10 and later made the constructor without an image obsolete.
28. Reset the data with Respawn in `[TestInitialize]` and mark the database test classes `[DoNotParallelize]`. A reset takes milliseconds. A new container takes seconds.
29. Read the address from the container: `GetConnectionString()`, `Hostname`, `GetMappedPublicPort()`. Fixed ports, `localhost`, and fixed container names collide on CI agents.
30. Override the connection string in `ConfigureWebHost` with `builder.UseSetting("ConnectionStrings:Orders", connectionString)`. `Program.cs` reads configuration before `ConfigureAppConfiguration` runs.
31. Do not use `WithReuse(true)`, and do not turn off the Resource Reaper. Reuse is experimental and leaves containers after the run.
32. Do not use the EF Core in-memory provider as a test database. Microsoft discourages it: no transactions, no raw SQL, other query results.

## Never write

| Wrong | Correct |
|-------|---------|
| `[ExpectedException(typeof(ArgumentException))]` | `Assert.ThrowsExactly<ArgumentException>(() => sut.Run());` |
| `Assert.ThrowsException<T>(...)` | `Assert.ThrowsExactly<T>(...)` or `Assert.Throws<T>(...)` |
| `Assert.IsInstanceOfType<T>(x, out var t)` | `var t = Assert.IsInstanceOfType<T>(x);` |
| `[DataTestMethod]` | `[TestMethod]` |
| `Assert.AreEqual(a, b, "Got {0}", x)` | `Assert.AreEqual(a, b, $"Got {x}")` |
| `Microsoft.NET.Test.Sdk` + `MSTest.TestAdapter` + `coverlet.collector` | `<Project Sdk="MSTest.Sdk">` and `dotnet test --coverage` |
| `<PackageReference Include="FluentAssertions" Version="8.*" />` | `Version="[7.2.2,8.0.0)"`, or AwesomeAssertions 9 |
| FA 7: `act.Should().Throw();` (no type) | `act.Should().Throw<Exception>();` |
| FA 7: `AssertionConfiguration.Current.Equivalency.Modify(...)` | `AssertionOptions.AssertEquivalencyUsing(...)` |
| FA 7: `options.ExcludingMembersNamed("Id")` | `options.Excluding((IMemberInfo m) => m.Name == "Id")` |
| FA 7: `PreferringRuntimeMemberTypes()` | `RespectingRuntimeTypes()` |
| AwesomeAssertions: `response.Should().HaveStatusCode(...)` | `response.StatusCode.Should().Be(HttpStatusCode.OK);` |
| `new PostgreSqlBuilder().WithImage("postgres:15")` | `new PostgreSqlBuilder("postgres:18.6-alpine")` |
| `WithPortBinding(5432, 5432)` and `Host=localhost` | `container.GetConnectionString()` |
| One container per test or per class | One per assembly, Respawn reset per test |
| `UseInMemoryDatabase("test")` in `WebApplicationFactory` | Real database in a container |
| `Thread.Sleep(1000)` to wait for a timer | `_time.Advance(TimeSpan.FromSeconds(1));` |
| `Mock<DbSet<Order>>` | Real database in a container |
| Tests for getters, setters, and constructors | Tests for rules, branches, and error paths |

## Check

Run these commands from the repository root. Fix each error, then run them again.

1. To build with the analyzers, run `dotnet build -warnaserror`. Pass: 0 warnings, 0 errors.
2. To run the tests, run `dotnet test`. With the MTP runner, add `--project <path>` for one project. Pass: `Test run summary: Passed!`. When you added or changed an integration test, run the suite 3 times. Pass: 3 times `Passed!`. A test that fails one time in 3 is flaky. Fix the shared state or the timing, then run it 3 times again.
3. To collect coverage, run `dotnet test --coverage --coverage-output-format cobertura`. Pass: a `*.cobertura.xml` file in `TestResults/`. There is no gate (TEST-11).
4. To run the rule check, run `python3 <skill folder>/scripts/stackcheck.py --root <repo>`. In an existing project, add `--changed <base branch>`. Pass: 0 errors.
5. After a change to the test stack, run one test that fails on purpose (`1.Should().Be(2)`), then remove it. Pass: the output shows "Expected ... to be 2, but found 1", not "threw exception". This proves the FA 7 bridge.
6. Optional: to find tests that check nothing, run Stryker.NET on one critical project (`references/test-design.md`).

## Review checklist

The full list is in `references/review-checklist.md`.

| # | Check | How |
|---|-------|-----|
| 1 | FluentAssertions is below 8. Moq is not 4.20.0 or 4.20.1. | tool (TST-PKG-FA8, TST-PKG-MOQ-SPONSORLINK) |
| 2 | FA 7 with MSTest 4: the `[AssemblyInitialize]` sets `Services.ThrowException`. | tool (TST-PKG-FA7-BRIDGE lists the projects), judge |
| 3 | No removed MSTest 3 APIs. | tool (TST-EXPECTED-EXCEPTION, TST-THROWS-EXCEPTION, compiler, MSTEST0044) |
| 4 | `global.json` selects Microsoft.Testing.Platform. Analysis mode is `Recommended`. | tool (TST-GLOBALJSON-MTP), judge |
| 5 | Each assembly declares its parallel mode. | tool (MSTEST0001) |
| 6 | Async tests flow `TestContext.CancellationToken`. `[Timeout]` is cooperative. | tool (MSTEST0049, MSTEST0045) |
| 7 | No sleep or real delay in tests. | tool (TST-THREAD-SLEEP, TST-TASK-DELAY) |
| 8 | Each test checks one behavior, and the name gives method, scenario, and result. | judge |
| 9 | The test asserts an outcome, not only "no exception". | judge (Stryker.NET can confirm) |
| 10 | The tests cover the critical core: rules, branches, error paths. No tests for trivial members. | judge |
| 11 | `BeEquivalentTo` has `WithStrictOrdering` where order matters, and `ComparingByMembers<T>` for classes with `Equals`. | judge |
| 12 | Mocks only at boundaries. `Received()` only when the call is the behavior. | judge |
| 13 | Containers have a pinned image, start once per assembly, and use random ports. | tool (TST-TC-NO-IMAGE, TST-TC-LATEST, TST-TC-FIXED-PORT), judge |
| 14 | No container reuse and no EF Core in-memory provider. | tool (TST-TC-REUSE, TST-PKG-EF-INMEMORY) |
| 15 | No logic in test bodies. Cases use `[DataRow]` or `[DynamicData]`. | judge |

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/mstest-4.md`: read before you set up a test project, a runner, an analyzer setting, or fix MSTest 3 code.
- `references/fluentassertions-7.md`: read before you write assertions in a project with FluentAssertions 6 or 7.
- `references/awesomeassertions.md`: read before you write assertions in a project with AwesomeAssertions.
- `references/testcontainers.md`: read before you write an integration test with a container, Respawn, or `WebApplicationFactory`.
- `references/test-design.md`: read when you decide which tests a class or an API needs, or for coverage and mutation testing.
- `references/examples/`: compiled and passing sample projects. Copy from them.
- `references/decisions.md`: read when new code needs a decision with an ID from the defaults table.
- `references/review-checklist.md`: read before a code review of test code.
- `references/sources.md`: the primary sources and the date of each check.
- `references/language.md`: the rules for all text that is not code.
