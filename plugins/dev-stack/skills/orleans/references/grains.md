# Grains, serialization, state, timers, and streams

Detail for the rules of `SKILL.md`. Each code block compiled on SDK 11.0.100 with Orleans 10.4.2, unless a line says "not verified".

## Contents

- Grain interface and grain
- Safe and unsafe changes in a live cluster
- State
- Timers
- Reminders
- Streams
- Observers and IAsyncEnumerable
- Reentrancy
- Placement

## Grain interface and grain

```csharp
[Alias("Shop.ICartGrain")]
public interface ICartGrain : IGrainWithStringKey
{
    [Alias("AddItem")] Task<int> AddItemAsync(string sku, int quantity, CancellationToken cancellationToken = default);
    [ReadOnly, Alias("Count")] Task<int> CountAsync();
}

[GenerateSerializer, Alias("Shop.CartLine")]
public sealed record CartLine([property: Id(0)] string Sku, [property: Id(1)] int Quantity);

[GenerateSerializer, Alias("Shop.CartState")]
public sealed class CartState
{
    [Id(0)] public Dictionary<string, int> Lines { get; set; } = [];
}

public sealed class CartGrain(
    [PersistentState("cart", "store")] IPersistentState<CartState> state) : Grain, ICartGrain
{
    public async Task<int> AddItemAsync(string sku, int quantity, CancellationToken cancellationToken = default)
    {
        state.State.Lines[sku] = state.State.Lines.GetValueOrDefault(sku) + quantity;
        await state.WriteStateAsync(cancellationToken); // written before the call returns (ORL-04)
        return state.State.Lines.Count;
    }

    public Task<int> CountAsync() => Task.FromResult(state.State.Lines.Count);
}
```

A POCO grain implements `IGrainBase` and gets its context from the constructor:

```csharp
public sealed class PingGrain(IGrainContext context) : IGrainBase, IPingGrain
{
    public IGrainContext GrainContext { get; } = context;
    public Task<string> PingAsync() => Task.FromResult(GrainContext.GrainId.ToString());
}
```

## Safe and unsafe changes in a live cluster

During a rolling deploy, old and new silos call each other, and the new silo reads the state that the old silo wrote.

| Change | Messages between old and new silos | Stored state |
|--------|------------------------------------|--------------|
| Add a member with a new `[Id]` | Safe. The old silo skips the new field. | Safe. The new member reads its default from old records. |
| Remove a member | Safe. Never reuse its `[Id]`. | Safe. |
| Rename a member, same `[Id]` | Safe. | Unsafe with the JSON serializer: the value reads as its default. |
| Change the `[Id]` of a member | Unsafe. | Unsafe with the binary serializer. |
| Change the type of a member | Unsafe. | Unsafe. |
| Rename a type that has an `[Alias]` | Safe. | Check the `$type` names in stored JSON. |
| Rename a type without an `[Alias]` | Unsafe. | Unsafe when the stored JSON has `$type`. |
| Add a grain method | Safe for old callers. A new caller fails on an old silo until all silos run the new version. | Not used. |
| Remove a grain method, or change its parameters | Unsafe for callers on the old version. Do it in a second deploy. | Not used. |
| Rename a grain class without `[GrainType]` | The grain type changes. | The storage key changes, so the state looks empty. Not verified by build: from the grain identity docs. |
| Change `ServiceId` or the provider name | Not used. | The state looks empty. |
| Change the storage serializer | Not used. | Unsafe unless the new serializer reads the old format. |

The default grain storage serializer of most providers is JSON with Newtonsoft.Json. Since 10.3, it resolves `$type` only for allowed types. The wire format uses the `[Id]` values. So a state type needs both stable names and stable `[Id]` values.

## State

- One grain can have more than one `[PersistentState]` parameter, each with its own name.
- `state.RecordExists` is `false` before the first write.
- `ClearStateAsync` removes the record.
- `Grain<TState>` still compiles with no warning in 10.4.2. Keep it in existing grains (`existing-code.md`).

Flush timer with the loss window in the code (ORL-04):

```csharp
public override Task OnActivateAsync(CancellationToken cancellationToken)
{
    // A silo crash loses the views since the last flush (at most 30 seconds). Accepted in ORL-04.
    this.RegisterGrainTimer(FlushAsync, new GrainTimerCreationOptions
    {
        DueTime = TimeSpan.FromSeconds(30),
        Period = TimeSpan.FromSeconds(30),
    });
    return Task.CompletedTask;
}

public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken) =>
    FlushAsync(cancellationToken); // runs at a normal deactivation, not after a crash
```

## Timers

- `RegisterGrainTimer` returns `IGrainTimer`. Dispose it to stop the timer, or call `Change` to set a new due time and period.
- An overload takes a state argument: `RegisterGrainTimer(callback, state, options)`.
- `Interleave = false` (the default): the callback waits for the running call, and no call starts during the callback.
- `Interleave = true`: the old `RegisterTimer` behavior. The callback can run between the `await` points of a call.
- The obsolete message of `RegisterTimer` gives the migration: `this.RegisterGrainTimer(callback, state, new() { DueTime = dueTime, Period = period, Interleave = true })`.

## Reminders

The silo needs `Microsoft.Orleans.Reminders` and a reminder provider (`UseRedisReminderService`, `UseAdoNetReminderService`, or `UseInMemoryReminderService` for tests).

```csharp
public sealed class CartGrain(...) : Grain, ICartGrain, IRemindable
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken) =>
        await this.RegisterOrUpdateReminder("expire", TimeSpan.FromHours(1), TimeSpan.FromHours(1));

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await state.ClearStateAsync();
        var reminder = await this.GetReminder("expire");
        if (reminder is not null) await this.UnregisterReminder(reminder);
    }
}
```

## Streams

```csharp
// Producer, in a grain
var stream = this.GetStreamProvider("events").GetStream<CartLine>(StreamId.Create("cart", this.GetPrimaryKeyString()));
await stream.OnNextAsync(line);

// Consumer with an implicit subscription
[ImplicitStreamSubscription("cart")]
public sealed class CartAuditGrain : Grain, ICartAuditGrain, IAsyncObserver<CartLine>
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var stream = this.GetStreamProvider("events").GetStream<CartLine>(StreamId.Create("cart", this.GetPrimaryKeyString()));
        await stream.SubscribeAsync(this);
    }
    // OnNextAsync, OnCompletedAsync, OnErrorAsync
}
```

- Silo setup for tests: `silo.AddMemoryStreams("events")` and `silo.AddMemoryGrainStorage("PubSubStore")`. The silo stores explicit subscriptions in `PubSubStore`.
- 10.4 adds `StreamSubscriptionStartPosition.Latest` and `EarliestAvailable` (from the release notes, not verified by build).

## Observers and IAsyncEnumerable

- An observer interface derives from `IGrainObserver`. The grain keeps the subscribers in an `ObserverManager<T>` (namespace `Orleans.Utilities`) with an expiry, and the client subscribes again before the expiry.
- A grain method can return `IAsyncEnumerable<T>` for a large result. Mark the token parameter `[EnumeratorCancellation]`.

```csharp
public async IAsyncEnumerable<CartLine> ItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    foreach (var (sku, quantity) in state.State.Lines)
    {
        yield return new CartLine(sku, quantity);
    }
}
```

## Reentrancy

| Attribute | Where | Effect |
|-----------|-------|--------|
| `[ReadOnly]` | Interface method | Calls to read-only methods can interleave with each other. |
| `[AlwaysInterleave]` | Interface method only (the analyzer rejects it on the class method) | This method interleaves with each call. |
| `[Reentrant]` | Grain class | Each call can interleave at each `await`. |

A call cycle (A calls B, B calls A) on grains that are not reentrant waits until the call times out. Fix the design first: let B return a result, or move the shared work into one grain.

## Placement

- Default since 9.2: `ResourceOptimizedPlacement`.
- Attributes: `[PreferLocalPlacement]`, `[HashBasedPlacement]`, `[ActivationCountBasedPlacement]`, `[RandomPlacement]`.
- `[Immovable]` stops automatic moves. `this.MigrateOnIdle()` asks for a move. `IGrainMigrationParticipant` keeps in-memory data during a move.
- Activation repartitioning (`AddActivationRepartitioner`, ORLEANSEXP001) and activation rebalancing (ORLEANSEXP002) are experimental. In the build check, `AddActivationRepartitioner` gave error ORLEANSEXP001 until a suppression of that ID.
