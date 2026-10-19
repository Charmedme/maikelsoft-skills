# EasyNetQ 8

Read this file only when the project uses EasyNetQ. New flows use Wolverine (MSG-04). The API was compiled against EasyNetQ 8.1.11. The defaults come from the EasyNetQ documentation (checked on 2026-10-16).

## Contents

- Version gate
- Registration
- Subscribe
- What EasyNetQ does not do
- Old code

## Version gate

- EasyNetQ 8.x: this file.
- EasyNetQ 9.0 is an alpha (9.0.0-alpha.3). Do not adopt it. stackcheck rule RMQ-PKG-EASYNETQ-ALPHA warns.
- EasyNetQ 7 and older use RabbitMQ.Client 6. Keep them until the task asks for an upgrade.

## Registration

```csharp
services.AddEasyNetQ("host=rabbitmq;publisherConfirms=true;prefetchCount=20;consumerDispatcherConcurrency=1;name=billing-worker");
```

- `publisherConfirms=true`: off by default. Without it, a publish during a connection failure can get lost.
- `consumerDispatcherConcurrency`: by default it follows `prefetchCount` (50). Then 50 messages run in parallel and out of order. Set it on purpose: 1 for order, a higher value only for thread-safe handlers where order does not matter.
- `name=`: the connection name in the management UI.

## Subscribe

```csharp
await using var subscription = await bus.PubSub.SubscribeAsync<OrderPlacedV1>(
    "billing",
    (message, token) => Task.CompletedTask,
    c => c.WithQueueType(QueueType.Quorum).WithPrefetchCount(20),
    ct);
```

- A subscription result is `IAsyncDisposable`: dispose it with `await using`, or keep it for the life of the service.
- `WithQueueType(QueueType.Quorum)`: EasyNetQ declares classic queues unless you say so.
- Do not call `WithAutoAck()` for business messages (stackcheck rule RMQ-AUTOACK).
- EasyNetQ names queues from the type name and the subscription ID. Pin the names with `WithQueueName` when another team reads the topology (MSG-06).

## What EasyNetQ does not do

- No outbox: write the outbox yourself (`outbox.md`), or use Wolverine.
- No retry policy: an exception in the handler goes to the EasyNetQ error queue at once. Build the retry, or classify in the handler.
- No delayed messages without a scheduler. Use the default DLX plus TTL scheduler. `UseDelayedExchangeScheduler()` needs the delayed message plugin (stackcheck rule RMQ-DELAYED-PLUGIN).

## Old code

| Old | EasyNetQ 8 |
|-----|-----------|
| `RabbitHutch.CreateBus("host=...")` | `services.AddEasyNetQ("host=...")` and inject `IBus` |
| `bus.Subscribe<T>(...)` (sync) | `await bus.PubSub.SubscribeAsync<T>(...)` |
| `bus.Publish(message)` | `await bus.PubSub.PublishAsync(message, ct)` |
