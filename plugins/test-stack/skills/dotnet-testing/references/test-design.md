# Test design

How to decide which tests to write, how to shape them, and how to measure them. Checked on 2026-10-16.

## Contents

- Which tests first
- What not to test
- Unit or integration test
- Test doubles
- Time
- Test data
- Shape of a test
- Coverage
- Mutation testing
- Flaky tests

## Which tests first

Start with the code where a bug costs the most. Write the tests in this order:

1. **Business rules and calculations** with branches: prices, discounts, tax, customs values, state changes. Test each branch and each boundary (the value at the limit, one below, one above).
2. **Error paths**: invalid input, a missing entity, a conflict, a forbidden state change. Assert the exception type or the error result.
3. **The contract of each endpoint**: status code, response shape, validation errors. Use an integration test (TEST-07).
4. **Queries and writes** against the real database: filters, paging, concurrency, constraints.
5. **Wiring that the compiler cannot check**: one test that starts the host (DI validation), one test per message handler.

Done when each rule in the change has a test that fails if you break the rule.

## What not to test

| Do not test | Reason |
|-------------|--------|
| Auto-properties, getters, setters, constructors that only assign | They cannot fail on their own. |
| Framework behavior: model binding, the DI container, EF change tracking | The framework has its own tests. |
| Generated code, mapping one field at a time | A compile error or an integration test finds the bug. |
| Log calls | They are not behavior. Test a log only when an audit rule requires it. |
| Private methods | Test them through the public method that uses them. |
| The call order inside a class | The test breaks on each refactor. |

## Unit or integration test

- **Unit test**: domain logic with branches, pure functions, a class with fakes for its boundaries. Runs in milliseconds, in parallel at method level.
- **Integration test**: everything that crosses a process boundary: HTTP pipeline, serialization, database queries, migrations, message handlers. `WebApplicationFactory` plus Testcontainers.
- For an API, most bugs live in wiring, serialization, and queries. Put most of the effort in integration tests and keep unit tests for domain logic with branches. Microsoft's EF Core guidance also says to test against the production database engine.

## Test doubles

| Double | What it is | Use for |
|--------|------------|---------|
| Real object | The production class | Value objects, domain logic, pure services |
| Fake | A small working copy, for example an in-memory store | A repository or a port with simple behavior |
| Stub | Returns fixed answers | A query that the test controls |
| Mock | Checks that a call happened | Only when the call is the behavior (the code sends an email or publishes a message) |

Rules:

- Mock only at a boundary: clock, network, message bus, payment provider, file system.
- Do not mock `DbContext` or `DbSet`. Use the real database in a container.
- Check one interaction per test, and only the one that is the behavior. `Received()` on each collaborator makes the test fragile.
- NSubstitute 6.2.0 is the default for new code. Keep Moq 4.20.2 or later in a repo that has it.

A hand-written fake (compiled: `examples/tests/Shop.UnitTests/FakeOrderStore.cs`):

```csharp
// A hand-written fake: simpler than a mock when the test needs real store behavior.
internal sealed class FakeOrderStore : IOrderStore
{
    private readonly Dictionary<int, Order> _orders = [];

    public IReadOnlyCollection<Order> Saved => _orders.Values;

    public void Add(Order order) => _orders[order.Id] = order;

    public Task<Order?> FindAsync(int id, CancellationToken cancellationToken)
        => Task.FromResult(_orders.GetValueOrDefault(id));

    public Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}
```

NSubstitute at a boundary (compiled: `examples/tests/Shop.UnitTests/OrderServiceTests.cs`):

```csharp
[TestMethod]
public async Task CloseAsync_OpenOrder_SavesClosedOrder()
{
    _store.FindAsync(7, Arg.Any<CancellationToken>()).Returns(new Order(7, 10m, null, []));

    await _sut.CloseAsync(7, TestContext.CancellationToken);

    await _store.Received(1).SaveAsync(Arg.Is<Order>(o => o.ClosedAt == Now), Arg.Any<CancellationToken>());
}
```

## Time

Inject `TimeProvider` in production code. In tests, use `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing` and move time with `Advance`. The test runs in microseconds and gives the same result each time.

```csharp
[TestMethod]
public void IsExpired_AtHoldTime_ReturnsTrue()
{
    var reservation = new Reservation(_time.GetUtcNow(), _time);
    _time.Advance(Reservation.HoldTime);

    bool expired = reservation.IsExpired;

    expired.Should().BeTrue();
}
```

`FakeTimeProvider` also fires timers that `CreateTimer` made, when `Advance` passes their due time.

## Test data

- Use the smallest data that proves the behavior. Extra data hides what the test is about.
- Give magic values a name: `private static readonly DateTimeOffset Now = ...;`.
- Use inline `new` for data that one test needs. When the same setup appears in two tests, add a factory method or a small builder (TEST-06). With records, `Order.Valid() with { Total = 0m }` is a cheap builder.
- Do not use random data in a unit test. A failure must repeat. When you need many rows (paging, performance), use Bogus with a fixed seed.

## Shape of a test

- Name: `Method_Scenario_ExpectedResult`. Class: `<ClassUnderTest>Tests`.
- Arrange, Act, Assert, with a blank line between the parts. One Act line.
- One behavior per test. Several assertions are fine when they describe one outcome. Wrap them in `AssertionScope` to see all failures at once.
- No `if`, loops, or `try` in the test body. Use data rows.
- Assert an outcome. A test without an assertion only proves that no exception occurred.

## Coverage

- Collect coverage with the Microsoft collector of `MSTest.Sdk`: `dotnet test --coverage --coverage-output-format cobertura`. Show the report in the PR (TEST-11).
- Set no percentage gate. Microsoft's unit testing guide says a high percentage does not show quality and a goal can do harm. A gate makes people write tests without assertions.
- Use the report to find uncovered branches in changed, critical code. Ask why each branch has no test.
- When an audit requires a number, put a low floor on changed code only.

## Mutation testing

Stryker.NET changes the code (for example `>` to `>=`) and runs the tests. A mutant that survives shows code that runs in a test but that no assertion checks.

- Run it on demand on one critical domain project (TEST-12). It is slow, so it does not belong in each PR build.
- Stryker.NET 5.0.0 (Apache-2.0) supports Microsoft.Testing.Platform. Install it as a local tool after the human approves it: `dotnet tool install dotnet-stryker`.
- Run it from the test project folder: `dotnet stryker --project Shop.csproj --reporter html`. Add `--since:main` to mutate only changed code. `--break-at <score>` fails the run below a score.
- Write the score in the PR or the ADR. Add tests for the surviving mutants that matter.

## Flaky tests

- No automatic retry (TEST-18). A retry hides a race condition.
- Fix a flaky test the same day, or mark it `[Ignore("Flaky: <reason>, <issue link>")]` and open an issue.
- Common causes: shared state in parallel tests, real time, a fixed port, a container without a wait strategy, test order. MSTEST0074 to MSTEST0077 find parallel tests that change global state without `[ResourceLock]`, the current folder, or the culture. They also find shared file paths.
