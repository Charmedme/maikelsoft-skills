# Async, cancellation, and time

Detail for rules 16 to 26 of `SKILL.md`. All code compiles on `net10.0`. The tests ran with MSTest 4.5.1 and `Microsoft.Extensions.TimeProvider.Testing` 10.11.0.

## Contents

- Async from top to bottom
- Cancellation
- ConfigureAwait
- Task or ValueTask
- Async streams
- Concurrency
- TaskCompletionSource and CancellationTokenSource
- Time with TimeProvider
- Test time with FakeTimeProvider
- Traps

## Async from top to bottom

- When one method in a call chain becomes async, make each caller async too. A partly async chain blocks threads and is worse than a fully synchronous one.
- Do not block on a task: `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `Task.WaitAll`, `Task.WaitAny`. Blocking holds a thread-pool thread for the full wait. Under load the pool starves: the app stops, the CPU stays low. With a synchronization context (WPF, WinForms, old ASP.NET), it can deadlock.
- Exception: a `Main` method or a test can be `async Task`. No place in modern .NET needs a block.
- Do not write `async void`. The caller cannot await it, and an exception in it stops the process. Only an event handler with the `(object? sender, EventArgs e)` signature can be `async void`. Put a `try`/`catch` around its body.
- An async method returns an awaitable type and has the `Async` suffix (SOL-08).
- Do not return a task from inside a `using` block without `await`. The object is disposed before the task completes.

## Cancellation

```csharp
public async Task<Order> CloseAsync(Guid id, CancellationToken cancellationToken)
{
    var order = await store.FindAsync(id, cancellationToken)
        ?? throw new KeyNotFoundException($"Order {id} does not exist.");

    if (order.ClosedAt is not null)
    {
        throw new InvalidOperationException($"Order {id} is already closed.");
    }

    var closed = order with { ClosedAt = time.GetUtcNow() };
    await store.SaveAsync(closed, cancellationToken);
    return closed;
}
```

- Put `CancellationToken cancellationToken` last in each async method that does I/O. Pass it to each call that accepts one (CA2016).
- A public library method can use `CancellationToken cancellationToken = default`. In app code, make it required, so that a caller cannot forget it.
- Check `cancellationToken.ThrowIfCancellationRequested()` in long CPU loops. I/O methods check the token themselves.
- Catch `OperationCanceledException` only to stop cleanly when your own token was cancelled. Do not log it as an error. A general catch uses a filter: `catch (Exception ex) when (ex is not OperationCanceledException)`.

## ConfigureAwait

- App code (ASP.NET Core, workers, console): no `ConfigureAwait(false)`. These apps have no synchronization context, so it changes nothing and adds noise.
- A reusable library package: use `ConfigureAwait(false)` on each `await`. The library does not know if a UI or legacy context calls it.
- CA2007 enforces it. `dev-stack:dotnet-analyzers` turns CA2007 on for libraries only.

```csharp
public static async Task<string> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
{
    using var reader = new StreamReader(stream, leaveOpen: true);
    return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
}
```

## Task or ValueTask

- Return `Task` or `Task<T>` by default.
- Use `ValueTask<T>` only when a profiler shows a hot path that often completes synchronously (for example a cache hit).
- Await a `ValueTask` one time only. Do not store it, do not await it two times, do not call `.Result` before it completes. The result is undefined. When you need a task to share, call `.AsTask()` one time.

```csharp
public static ValueTask<int> CachedLengthAsync(string? cached, Func<Task<int>> load) =>
    cached is not null ? ValueTask.FromResult(cached.Length) : new ValueTask<int>(load());
```

## Async streams

```csharp
public async IAsyncEnumerable<Order> OpenOrdersAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var order in store.ReadAllAsync(cancellationToken))
    {
        if (order.ClosedAt is null)
        {
            yield return order;
        }
    }
}
```

- Return `IAsyncEnumerable<T>` for a large or streamed result, instead of `Task<List<T>>`.
- Mark the token with `[EnumeratorCancellation]`. A caller can then pass a token with `.WithCancellation(token)`.
- On `net10.0`, `System.Linq.AsyncEnumerable` is in the core libraries (`Where`, `Select`, `ToListAsync`, and more). Remove the `System.Linq.Async` package. Versions before 7.0 give ambiguous calls.

## Concurrency

- Run independent I/O with `Task.WhenAll`. Start the tasks first, then await them together.
- Limit fan-out over a large list. `Task.WhenAll` over 10,000 calls to one service overloads it.

```csharp
public async Task CloseManyAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken)
{
    var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken };
    await Parallel.ForEachAsync(ids, options, async (id, ct) => await CloseAsync(id, ct));
}
```

- Use `Task.Run` only to move CPU work off a thread that must stay free (a UI thread). Do not wrap I/O in `Task.Run`. Do not combine `TaskCreationOptions.LongRunning` with async code.
- Do not start work and forget it (`_ = SaveAsync();`). Exceptions are lost and the host can stop during the work. In a host, use a queue that a background service reads (`dev-stack:aspnetcore`).
- Use `SemaphoreSlim.WaitAsync` for an async lock. Do not `await` inside `lock`.
- Use `Channel<T>` for producer and consumer queues in one process.

## TaskCompletionSource and CancellationTokenSource

```csharp
public sealed class ReadySignal
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WhenReady => _ready.Task;

    public void MarkReady() => _ready.TrySetResult();
}
```

- Without `RunContinuationsAsynchronously`, the code after `await WhenReady` runs inside `TrySetResult`, on the thread of the caller. This can deadlock or hold a lock for too long.
- Dispose each `CancellationTokenSource` that you create (`using var`). A linked source holds a registration on its parent token until disposal.

## Time with TimeProvider

`TimeProvider` is in the BCL since .NET 8. Inject it, and register `TimeProvider.System` in the host.

| Need | Write |
|------|-------|
| Current instant | `time.GetUtcNow()` (a `DateTimeOffset` in UTC) |
| Local time for display | `time.GetLocalNow()` |
| Elapsed time | `time.GetTimestamp()` and `time.GetElapsedTime(start)` |
| Delay | `await Task.Delay(delay, time, cancellationToken)` |
| Periodic work | `using var timer = new PeriodicTimer(period, time);` |
| Timeout | `using var cts = new CancellationTokenSource(timeout, time);` |
| Task timeout | `await task.WaitAsync(timeout, time, cancellationToken)` |
| Callback timer | `time.CreateTimer(callback, state, dueTime, period)` (dispose it) |

```csharp
public async Task<Order?> FindWithTimeoutAsync(Guid id, TimeSpan timeout, CancellationToken cancellationToken)
{
    using var timeoutSource = new CancellationTokenSource(timeout, time);
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
    return await store.FindAsync(id, linked.Token);
}
```

- Do not read `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now`, `DateTimeOffset.UtcNow`, or `DateTime.Today` in logic. Tests cannot control them.
- Do not write an own `IClock`. `ISystemClock` is obsolete since .NET 8.
- Store instants as UTC `DateTimeOffset`. Use `DateOnly` for dates without a time and `TimeOnly` for a time of day.

## Test time with FakeTimeProvider

`FakeTimeProvider` is in `Microsoft.Extensions.TimeProvider.Testing` (MIT, 10.11.0 on 2026-10-16). It starts at 2000-01-01 UTC unless you give a start time. `Advance` moves the time and runs the timers and delays that are due.

```csharp
[TestMethod]
public void IsExpired_after_max_age_returns_true()
{
    var time = new FakeTimeProvider(s_start);
    var service = new OrderService(new InMemoryStore(), time);
    var order = new Order(Guid.NewGuid(), time.GetUtcNow(), null, 10m);

    time.Advance(TimeSpan.FromHours(2));

    Assert.IsTrue(service.IsExpired(order, TimeSpan.FromHours(1)));
}

[TestMethod]
public async Task WaitAsync_completes_when_time_advances()
{
    var time = new FakeTimeProvider(s_start);
    var poller = new Poller(time);

    var wait = poller.WaitAsync(TimeSpan.FromMinutes(5), TestContext.CancellationToken);
    Assert.IsFalse(wait.IsCompleted);

    time.Advance(TimeSpan.FromMinutes(5));
    await wait;
}
```

- Give each test its own `FakeTimeProvider`. Do not mix `TimeProvider.System` and a fake in one test.
- Test the edges: midnight, month end, daylight saving time (`SetLocalTimeZone`), leap years.
- The test framework, assertions, and test layout belong to `dev-stack:dotnet-testing`.

## Traps

| Trap | Effect | Fix |
|------|--------|-----|
| `.Result` in a request path | Thread-pool starvation, the app hangs | `await` |
| `async void` timer or handler | Process stops on an exception | `async Task`, or a `try`/`catch` in the event handler |
| `Task.WhenAll` over a large list | The other service is overloaded | `Parallel.ForEachAsync` with a limit |
| `ValueTask` awaited two times | Undefined result | Await one time, or `.AsTask()` |
| `return task;` inside `using` | Object disposed before the work ends | `return await task;` |
| Async lambda passed to an `Action` parameter | The lambda becomes `async void` | Use an overload that takes `Func<Task>` |
| `TaskCompletionSource` without options | Continuation runs in `SetResult` | `RunContinuationsAsynchronously` |
| `DateTime.Now` stored in a database | Time zone lost, tests not repeatable | `TimeProvider.GetUtcNow()`, `DateTimeOffset` |
