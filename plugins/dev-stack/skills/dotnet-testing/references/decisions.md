# Decisions: .NET testing

The team decisions of this skill, in the order that a project meets them. Each entry gives the question for the human and the recommendation. Follow stack rule 4: when `docs/dev-team/decisions.md` has an accepted answer, use it. Else ask the human, and use the recommendation only when nobody can answer (mark it "assumed").

## Contents

- TEST-01. Unit test framework
- TEST-04. Test project layout
- TEST-02. Assertion library
- TEST-05. Test naming convention
- TEST-17. Test parallelization
- TEST-03. Mocking library
- TEST-07. Integration test strategy
- DATA-20. Test database strategy
- TEST-08. Database state between tests
- TEST-11. Coverage tool and gate
- TEST-18. CI test stages and flaky test policy
- TEST-06. Test data creation
- TEST-10. Architecture tests
- TEST-13. API contract tests
- TEST-09. Snapshot testing
- TEST-12. Mutation testing

Ask TEST-01, TEST-04, TEST-02, TEST-05, and TEST-17 together as one "test setup" question, with all recommendations filled in. Ask the others one by one when the work needs them.

## TEST-01. Unit test framework

- Question: Do we use MSTest 4 with `MSTest.Sdk` for all new .NET test projects?
- Ask when: the first .NET test project.
- Options: MSTest 4 with `MSTest.Sdk` and Microsoft.Testing.Platform: first-party runner, analyzers, coverage in one SDK. xUnit v3 (`xunit.v3`, MTP mode): most new adoption, class and collection fixtures. NUnit: mature, many attributes.
- Recommended: MSTest 4 with `MSTest.Sdk`. It is the current stack, it has analyzers and coverage in one SDK, and a second framework adds cost with no gain.
- Choose another option when: the repo already uses xUnit or NUnit (keep it). Or the team needs xUnit-style class and collection fixtures as a core pattern.
- Depends on: none.
- Revisit when: MSTest drops MTP support, or the team moves most code to a repo on another framework.
- Check: judge
- Detect: TST-DET-TEST-01

## TEST-04. Test project layout

- Question: Do we create one unit test project per production project, plus one integration test project per deployable service?
- Ask when: the first test project.
- Options: `X.UnitTests` per production project and `X.IntegrationTests` per host: unit tests stay free of Docker. One test project per layer: fewer projects. One test project for the solution: simplest, but slow and mixed.
- Recommended: `src/` and `tests/` folders, one `X.UnitTests` per production project that has logic, one `X.IntegrationTests` per web host. It is the Microsoft and eShop pattern, and the unit test run stays fast.
- Choose another option when: the solution has many tiny projects (one unit test project per bounded context is fine).
- Depends on: TEST-01.
- Revisit when: test build time grows above a few minutes, or a project gets split.
- Check: judge
- Detect: TST-DET-TEST-04

## TEST-02. Assertion library

- Question: For new test projects, do we use AwesomeAssertions 9, FluentAssertions 7.2.2, or plain MSTest `Assert`?
- Ask when: the first assertion in a new test project.
- Options: AwesomeAssertions 9: Apache-2.0 fork of FluentAssertions, FA 8.0 API, namespace `AwesomeAssertions`, MSTest 4 support since 9.3. FluentAssertions 7.2.2: last Apache-2.0 line, fixes only, needs the MSTest 4 bridge. FluentAssertions 8: commercial Xceed license for company use, not allowed. Shouldly 4.3: smaller API, no stable release since 2025-01. MSTest `Assert` only: no extra package, no object graph comparison.
- Recommended: AwesomeAssertions 9 for new repos. Keep FluentAssertions 7.2.2, pinned `[7.2.2,8.0.0)` with the bridge, in repos that already use FA. The skill supports both. AwesomeAssertions keeps the Apache-2.0 license, has active maintainers, supports MSTest 4 without a bridge, and matches the current docs that agents learn from.
- Choose another option when: the repo already uses FA 7. Keep it, or migrate with `dev-stack:migrate-fluentassertions` after the human approves. Use MSTest `Assert` when the team wants no extra packages. FA 8 is not an option: the human decided that a paid license is not allowed.
- Depends on: TEST-01.
- Revisit when: AwesomeAssertions has no release for a year, or MSTest `Assert` gets object graph equivalence.
- Check: tool (TST-PKG-FA8, TST-PKG-FA7-BRIDGE)
- Detect: TST-DET-TEST-02

## TEST-05. Test naming convention

- Question: Do we name tests `Method_Scenario_ExpectedResult`?
- Ask when: the first test method.
- Options: `Method_Scenario_ExpectedResult`: the documented .NET pattern. `Should_X_When_Y` or `Given_When_Then`: reads as a sentence. Plain sentence in `[TestMethod(DisplayName = "...")]`: for tests that product people read.
- Recommended: `Method_Scenario_ExpectedResult`, and the class name `<ClassUnderTest>Tests`. The test list documents the code, and a failure points at the behavior. It needs no tool.
- Choose another option when: the repo already uses another pattern, or product people own acceptance tests (sentence `DisplayName`).
- Depends on: TEST-01.
- Revisit when: the team adopts BDD tooling.
- Check: judge
- Detect: TST-DET-TEST-05

## TEST-17. Test parallelization

- Question: Do unit test projects run in parallel at method level, and integration test projects at class level with database classes in sequence?
- Ask when: the first test project (MSTEST0001 asks for an explicit choice).
- Options: `MethodLevel` for unit tests and `ClassLevel` plus `[DoNotParallelize]` for integration tests: fast and safe. All in sequence (MSTest default): slow, hides no state bugs. All at method level: fastest, breaks shared database tests.
- Recommended: method level for unit tests. Class level plus `[DoNotParallelize]` on database classes for integration tests.
- Choose another option when: a legacy suite has hidden shared state (start in sequence, fix the state, then run in parallel).
- Depends on: TEST-01, TEST-08.
- Revisit when: flaky failures appear only in parallel runs.
- Check: tool (MSTEST0001)
- Detect: TST-DET-TEST-17

## TEST-03. Mocking library

- Question: Do we use NSubstitute for mocks, and prefer hand-written fakes where they are simple?
- Ask when: the first test that needs a test double for an interface.
- Options: NSubstitute 6: short syntax, no `.Object`, analyzer package. Moq 4.21: most downloads, `Mock<T>` and `.Object`. FakeItEasy 9: `A.Fake<T>()`. Hand-written fakes only: no package, more code.
- Recommended: NSubstitute 6 plus `NSubstitute.Analyzers.CSharp`, and hand-written fakes for repositories, clocks (`FakeTimeProvider`), and simple ports. NSubstitute needs the least code, and the analyzer finds misuse at build time.
- Choose another option when: the repo already uses Moq 4.20.2 or later (keep it, SponsorLink is gone) or FakeItEasy.
- Depends on: TEST-01.
- Revisit when: NSubstitute has no release for a year.
- Check: tool (TST-PKG-MOQ-SPONSORLINK), judge
- Detect: TST-DET-TEST-03

## TEST-07. Integration test strategy

- Question: Do we test the API through `WebApplicationFactory` against a real database in Testcontainers, with one container per test assembly?
- Ask when: the first endpoint or the first database query.
- Options: `WebApplicationFactory<Program>` plus Testcontainers, one container per assembly: real engine, full pipeline. `WebApplicationFactory` plus SQLite or the EF in-memory provider: no Docker, other behavior. A shared long-lived test database: no Docker, tests collide. Docker Compose started by CI: works, but the tests do not control the lifetime.
- Recommended: `WebApplicationFactory<Program>` plus Testcontainers with a pinned image, one container per test assembly, connection string replaced in `ConfigureWebHost`. Put most API test effort here. Unit tests cover domain branches.
- Choose another option when: CI agents cannot run Docker (then a database per pipeline run, created and dropped by the pipeline).
- Depends on: TEST-01, TEST-04.
- Revisit when: the integration suite takes more than about 10 minutes.
- Check: tool (TST-TC-NO-IMAGE, TST-PKG-EF-INMEMORY), judge
- Detect: TST-DET-TEST-07

## DATA-20. Test database strategy

- Question: What database do integration tests run against?
- Ask when: the first test that touches data.
- Options: Testcontainers with the production engine and a Respawn reset: real behavior, milliseconds per reset. Testcontainers with a transaction per test, rolled back: faster, breaks when the code commits. SQLite in-memory: no Docker, other SQL dialect. EF InMemory provider: no SQL at all, Microsoft discourages it.
- Recommended: one Testcontainers container per test run, image pinned to the production tag, migrations applied once, Respawn reset before each test.
- Choose another option when: the code under test never commits and uses no execution strategy (transaction rollback per test is faster).
- Depends on: DATA-01 (database engine), DATA-17 (`dev-stack:efcore`).
- Revisit when: test run time becomes a problem (parallel containers per test class).
- Check: tool (TST-PKG-EF-INMEMORY, TST-TC-LATEST)
- Detect: TST-DET-DATA-20

## TEST-08. Database state between tests

- Question: Do we reset the database with Respawn before each test and run database test classes in sequence?
- Ask when: the second integration test that writes data.
- Options: Respawn `ResetAsync` before each test, database classes `[DoNotParallelize]`: simple, works through HTTP. Transaction per test, rolled back: fast, breaks with a second connection. One database per test class from a template: parallel, more setup. Unique data per test, no reset: fast, tests see each other's data.
- Recommended: Respawn reset plus database classes in sequence. A reset takes milliseconds, works through the full HTTP pipeline, and needs no special code in production.
- Choose another option when: the suite is slow and CPU is free (one database per class), or the tests only read (seed once).
- Depends on: TEST-07.
- Revisit when: the integration suite is the slowest CI stage.
- Check: judge
- Detect: TST-DET-TEST-08

## TEST-11. Coverage tool and gate

- Question: Do we collect coverage with the `MSTest.Sdk` built-in Microsoft collector, show it in CI, and set no percentage gate?
- Ask when: the first CI pipeline that runs tests.
- Options: Microsoft.Testing.Extensions.CodeCoverage (in the `MSTest.Sdk` default profile, `--coverage`): no extra package. coverlet (`coverlet.MTP` for MTP): open source license. No coverage. Any collector plus a gate (for example 80% line coverage).
- Recommended: the Microsoft collector through `MSTest.Sdk` (`dotnet test --coverage --coverage-output-format cobertura`), report in the PR, no gate. It needs no extra package and gives the signal without the gaming that a gate causes.
- Choose another option when: a customer or an audit requires a number (then a low floor on changed code only). Use `coverlet.MTP` when the team needs an OSI license for each tool. The Microsoft collector is closed source, free to use.
- Depends on: TEST-01, GIT-10.
- Revisit when: reviews often find untested logic in merged code.
- Check: judge
- Detect: TST-DET-TEST-11

## TEST-18. CI test stages and flaky test policy

- Question: Do all unit and integration tests run on each PR, and do we quarantine a flaky test instead of adding automatic retries?
- Ask when: the first CI pipeline, or the first flaky failure.
- Options: all unit and integration tests on each PR, E2E after merge, no retries, quarantine with `[Ignore("reason, issue link")]`. Automatic retry (`Microsoft.Testing.Extensions.Retry`): green builds, hidden races. Integration tests only at night (`[TestCategory]`): fast PRs, late feedback.
- Recommended: unit plus integration tests on each PR, E2E after merge, no automatic retry, quarantine with a reason and an issue.
- Choose another option when: the integration suite takes more than about 15 minutes (split by category, run the slow part after merge).
- Depends on: GIT-10, TEST-16.
- Revisit when: PR checks take longer than about 15 minutes.
- Check: judge
- Detect: TST-DET-TEST-18

## TEST-06. Test data creation

- Question: Do we build test data with small hand-written builders or factory methods, and add Bogus only for volume data?
- Ask when: the second test that needs the same object with many members.
- Options: hand-written builders or factory methods (`Invoice.Valid() with { ... }`): clear, deterministic. AutoFixture: generated objects, last release 2023. Bogus: realistic fake data. Inline `new` in each test: clear, repeats setup.
- Recommended: inline `new` for data that one test needs, and a hand-written builder or factory method per aggregate when the same setup appears twice. No library. Each test stays readable and gives the same result each time.
- Choose another option when: you need many rows for performance or paging tests (Bogus with a fixed seed), or you seed a demo database.
- Depends on: TEST-01.
- Revisit when: builders grow into a large second model of the domain.
- Check: judge
- Detect: TST-DET-TEST-06

## TEST-10. Architecture tests

- Question: Do we enforce layer rules with project references only, and add architecture tests later if needed?
- Ask when: the second production project (the first time a layer rule exists).
- Options: project references only: the compiler blocks a wrong dependency. ArchUnitNET (`TngTech.ArchUnitNET.MSTestV2`): rules inside one assembly, active. NetArchTest.Rules: popular, no release since 2021. None.
- Recommended: project references only. No tooling, and the build already fails on a wrong reference.
- Choose another option when: rules exist inside one project (vertical slices, a modular monolith in one assembly). Then ArchUnitNET, because NetArchTest is not maintained.
- Depends on: TEST-04.
- Revisit when: a review finds the same dependency violation twice.
- Check: judge
- Detect: TST-DET-TEST-10

## TEST-13. API contract tests

- Question: With one team that owns both the Angular client and the API, do we use OpenAPI diff checks instead of consumer-driven contract tests?
- Ask when: the first endpoint that the frontend calls.
- Options: OpenAPI document generated at build plus an oasdiff breaking-change check in CI: little setup. Pact (consumer-driven): strong for many teams, much setup. Integration tests that check each response shape: already there, no diff. None.
- Recommended: an OpenAPI document per version plus oasdiff `breaking` in CI, plus the integration tests from TEST-07. No Pact.
- Choose another option when: external consumers or other teams depend on the API, or services deploy apart and call each other.
- Depends on: TEST-07, the decisions of `dev-stack:aspnetcore-openapi`.
- Revisit when: a second consumer team appears.
- Check: judge
- Detect: TST-DET-TEST-13

## TEST-09. Snapshot testing

- Question: Do we skip snapshot testing for now, and assert explicit values instead?
- Ask when: the first test of a large output (a JSON response, a generated document, an OpenAPI file).
- Options: no snapshot library, explicit assertions and `BeEquivalentTo`. Verify (`Verify.MSTest`): strong tooling, binaries since 33.0.0 under a maintenance fee EULA. Snapshooter: smaller. A committed expected file compared with a plain string assert: no package.
- Recommended: no snapshot library. Use explicit assertions. For one large file, commit an expected file and compare. This avoids a new license and a new review habit.
- Choose another option when: many tests check large generated outputs (PDF text, emails, serializers). Then Verify, after the license check: Verify 33.0.0 and later binaries need a monthly fee for companies with revenue of US$10,000 or more a year. Verify 32.0.1 and earlier are MIT.
- Depends on: TEST-02.
- Revisit when: three or more tests compare large outputs.
- Check: judge
- Detect: TST-DET-TEST-09

## TEST-12. Mutation testing

- Question: Do we skip mutation testing in CI and run Stryker.NET by hand on critical domain code when we doubt the tests?
- Ask when: the first domain module with money, tax, or customs rules.
- Options: no mutation testing. Stryker.NET on demand on one project. Stryker.NET weekly in CI. Stryker.NET on each PR (`--since`).
- Recommended: on demand only, on the domain project, with a short note of the score in the ADR or PR. No CI job. Lowest cost for the main benefit.
- Choose another option when: the domain code is the core risk of the product (customs calculations): a weekly CI job on that project.
- Depends on: TEST-01.
- Revisit when: a production bug passes a test suite that has full coverage of the code.
- Check: judge
- Detect: TST-DET-TEST-12
