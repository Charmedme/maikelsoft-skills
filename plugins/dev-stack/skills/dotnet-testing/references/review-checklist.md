# Review checklist

The full list for a review of .NET test code. `tool` means a tool finds it: a rule ID of `rules.json`, a compiler ID, or an analyzer ID. `judge` means a reviewer reads the code. Run the tools first (stack rule 6): `dotnet build -warnaserror`, `dotnet test`, and `stackcheck.py`.

## Contents

- Project and runner
- Packages and licenses
- MSTest API
- Test design
- Assertions
- Test doubles and time
- Integration tests
- Parallel runs and CI

## Project and runner

| # | Check | How |
|---|-------|-----|
| 1 | The test project uses `MSTest.Sdk`, or the documented `Microsoft.NET.Sdk` exception for web SDK targets. | judge |
| 2 | `global.json` pins `MSTest.Sdk` under `msbuild-sdks`. | judge |
| 3 | `global.json` selects `Microsoft.Testing.Platform` for `dotnet test`. | tool (TST-GLOBALJSON-MTP) |
| 4 | `MSTestAnalysisMode` is `Recommended`, and CI builds with warnings as errors. | judge |
| 5 | Unit tests and integration tests are in separate projects. | judge |

## Packages and licenses

| # | Check | How |
|---|-------|-----|
| 6 | FluentAssertions is in `[7.0.0,8.0.0)`. No version 8 or later. | tool (TST-PKG-FA8) |
| 7 | FA 6 or 7 with MSTest 4: the one `[AssemblyInitialize]` sets `Services.ThrowException`. | tool (TST-PKG-FA7-BRIDGE lists each project to check), judge |
| 8 | Only one assertion library and one mock library in the repository. | judge |
| 9 | Moq, when present, is not 4.20.0 or 4.20.1. | tool (TST-PKG-MOQ-SPONSORLINK) |
| 10 | No `Microsoft.EntityFrameworkCore.InMemory` in test projects. | tool (TST-PKG-EF-INMEMORY) |
| 11 | No `coverlet.collector` or `Microsoft.NET.Test.Sdk` in an `MSTest.Sdk` project. | judge |
| 12 | Each new package has the approval of the human (stack rule 5). | judge |

## MSTest API

| # | Check | How |
|---|-------|-----|
| 13 | No `[ExpectedException]`. | tool (TST-EXPECTED-EXCEPTION, compiler CS0246) |
| 14 | No `Assert.ThrowsException` or `ThrowsExceptionAsync`. | tool (TST-THROWS-EXCEPTION, compiler CS0117) |
| 15 | No `[DataTestMethod]`. No format arguments in assert messages. | tool (MSTEST0044, compiler) |
| 16 | `Assert.AreEqual` has the expected value first and is not used on collections. | tool (MSTEST0017, MSTEST0065) |
| 17 | Async tests return `Task` and flow `TestContext.CancellationToken`. | tool (MSTEST0040, MSTEST0049) |
| 18 | Each `[Timeout]` sets `CooperativeCancellation = true`. | tool (MSTEST0045) |
| 19 | No static `TestContext`. | tool (MSTEST0024) |
| 20 | `[DynamicData]` returns tuples or `TestDataRow<T>`, without `DynamicDataSourceType`. | tool (MSTEST0052), judge |

## Test design

| # | Check | How |
|---|-------|-----|
| 21 | The tests cover the critical core: rules, branches, boundaries, error paths, endpoint contracts. | judge |
| 22 | No tests for trivial members, framework behavior, generated code, or private methods. | judge |
| 23 | Each test checks one behavior. | judge |
| 24 | The name is `Method_Scenario_ExpectedResult` (or the accepted TEST-05 pattern). | judge |
| 25 | Arrange, Act, Assert with blank lines. One Act line. | judge |
| 26 | No `if`, loops, or `try` in the test body. Cases use data rows. | judge |
| 27 | Each test asserts an outcome, not only "no exception". | judge (Stryker.NET can confirm) |
| 28 | Test data is minimal. Magic values have a name. No random data in unit tests. | judge |
| 29 | Test classes are `sealed`. Sync setup is in the constructor. | judge |

## Assertions

| # | Check | How |
|---|-------|-----|
| 30 | The code uses the API of the installed library (no FA 8 members with FA 7 or AwesomeAssertions 9.6). | tool (compiler) |
| 31 | `BeEquivalentTo` has `WithStrictOrdering` where order is part of the contract. | judge |
| 32 | `Excluding` on a class that overrides `Equals` comes with `ComparingByMembers<T>()`. | judge |
| 33 | The test asserts each exception on a delegate, with `ThrowExactly` where the exact type matters. | judge |
| 34 | `AssertionScope` groups facts of one outcome only. | judge |
| 35 | `[AssemblyInitialize]` sets the global equivalency options one time. | judge |

## Test doubles and time

| # | Check | How |
|---|-------|-----|
| 36 | Mocks only at boundaries. No mocked `DbContext` or `DbSet`. | judge (`dev-stack:efcore` EF-MOCK-DBSET finds `Mock<DbSet>`) |
| 37 | `Received()` only for a call that is the behavior. | judge |
| 38 | Time comes from `TimeProvider`. Tests use `FakeTimeProvider.Advance`. | judge (`dev-stack:csharp` CS-CLOCK finds clock reads in production code) |
| 39 | No `Thread.Sleep` or `Task.Delay` in tests. | tool (TST-THREAD-SLEEP, TST-TASK-DELAY, MSTEST0067 when on) |

## Integration tests

| # | Check | How |
|---|-------|-----|
| 40 | Each builder gets a pinned image in its constructor. | tool (TST-TC-NO-IMAGE, compiler CS0618) |
| 41 | No `latest` tags. The tag matches production. | tool (TST-TC-LATEST), judge |
| 42 | One container per assembly, started in `[AssemblyInitialize]`, disposed in `[AssemblyCleanup]`. | judge |
| 43 | Respawn reset before each test. Database classes are `[DoNotParallelize]`. | judge |
| 44 | No fixed host ports, no `localhost`, no fixed container names. | tool (TST-TC-FIXED-PORT), judge |
| 45 | Generic containers have a readiness wait strategy. | judge |
| 46 | No `WithReuse(true)`. No `TESTCONTAINERS_RYUK_DISABLED` outside Bitbucket. | tool (TST-TC-REUSE), judge |
| 47 | `ConfigureWebHost` sets the connection string with `UseSetting`. | judge |
| 48 | The schema comes from the real migrations, not `EnsureCreated`. | judge |

## Parallel runs and CI

| # | Check | How |
|---|-------|-----|
| 49 | Each assembly declares `[assembly: Parallelize]` or `[assembly: DoNotParallelize]`. | tool (MSTEST0001) |
| 50 | Parallel tests do not change culture, the current folder, or environment variables without `[ResourceLock]`. | tool (MSTEST0074 to MSTEST0077), judge |
| 51 | No automatic retry. A flaky test has `[Ignore("reason, issue link")]`. | judge |
| 52 | CI collects and shows coverage. No percentage gate unless the accepted TEST-11 says so. | judge |
