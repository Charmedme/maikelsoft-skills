---
name: wolverine
description: Messaging code with Wolverine 6 (WolverineFx) in .NET 9 and 10 services. Use when code or a review touches UseWolverine, message handlers, IMessageBus, InvokeAsync, SendAsync, PublishAsync, cascading messages, the durable outbox and inbox on EF Core with PostgreSQL or SQL Server, AutoApplyTransactions, IDbContextOutbox, error policies and retries, the dead-letter queue, local queues, scheduled messages, code generation (TypeLoadMode, codegen write), tracked session tests, or the Wolverine RabbitMQ transport. Not for raw RabbitMQ.Client or EasyNetQ code and broker topology (dev-stack:rabbitmq), DbContext design (dev-stack:efcore), Wolverine.HTTP endpoints, Marten, or MassTransit and NServiceBus.
metadata:
  verified: "2026-10-27"
  versions: "WolverineFx 6.56.0 (6.51.3 or later), JasperFx 2.96, RabbitMQ 4.3, EF Core 10, .NET 9 and 10"
---

# Wolverine

Wolverine 6 as a mediator, a local queue, and a message bus with a durable outbox and inbox on EF Core. Also the settings of its RabbitMQ transport. Broker rules (queue types, dead-letter policies, names) are in `dev-stack:rabbitmq`. DbContext rules are in `dev-stack:efcore`. Wolverine.HTTP and Marten are out of scope: minimal API endpoints (API-01) call Wolverine through `IDbContextOutbox<T>` or `InvokeAsync`.

## First

1. Read `references/stack-rules.md` and `references/messaging-principles.md`.
2. Find the versions: the `WolverineFx*` packages and the target framework in `*.csproj`, `Directory.Build.props`, or `Directory.Packages.props`.
   - Wolverine 6 on .NET 9 or 10: use this file.
   - Wolverine 5: read `references/wolverine5.md`. Version 5 gets fixes for severe bugs only, and its end of life is October 2026. Tell the human. Upgrade only when the task asks for it.
   - .NET 8: Wolverine 6 does not support it. Stay on 5.x, or the raw client (`dev-stack:rabbitmq`), and tell the human.
3. Check `docs/dev-team/decisions.md` for MSG-04 (Wolverine accepted), MSG-09 (outbox), and MSG-13 (code generation).

Done when you can name the Wolverine version, the target framework, the message store (PostgreSQL or SQL Server), and the transport.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`. Contracts, names, queue types, idempotency, wire format, and tracing (MSG-05, MSG-06, MSG-07, MSG-10, MSG-11, MSG-12) are in `dev-stack:rabbitmq`.

| Area | Default | ID |
|------|---------|----|
| Messaging library | Wolverine 6 for services that write to a database and publish. | MSG-04 |
| Mediator | No mediator. Wolverine `InvokeAsync` only when Wolverine is already in the service. | MSG-02 |
| Unit of work | `Policies.AutoApplyTransactions()` with the EF Core integration. | DATA-06 |
| Outbox and inbox | `PersistMessagesWithPostgresql` (or SQL Server), durable outbox on all senders, durable inbox on business listeners. | MSG-09 |
| Retries | `RetryWithCooldown(50ms, 250ms, 1s)`, then `ScheduleRetry(10s, 1min)` with jitter, then the error queue. Business and validation errors to the error queue at once. | MSG-08 |
| Dead letters | One dead-letter queue per service, `{service}.dlq`, with `CustomizeDeadLetterQueueing`. | MSG-06 |
| Code generation | `TypeLoadMode.Static` in production with `codegen write` in CI. `Dynamic` in development. `WolverineFx.RuntimeCompilation` in all builds. | MSG-13 |

Existing code keeps its style (stack rule 3).

## Rules

### Host and code generation

1. `TypeLoadMode.Dynamic` is the default and needs the package `WolverineFx.RuntimeCompilation`. Without it, the host does not start ("no IAssemblyGenerator (Roslyn) is registered").
2. For production, set `TypeLoadMode.Static`, run `dotnet run -- codegen write` in CI, and commit `Internal/Generated/` (MSG-13). Static mode without the generated files does not start (`MissingPreBuiltTypesException`). The command starts the host, so `Program.cs` must not connect to a database or broker before `RunJasperFxCommands(args)`.
3. Set `opts.ApplicationAssembly = typeof(Program).Assembly`. Then Wolverine looks for handlers in the app assembly, also when a test starts the app.

### Registrations

4. Register each `DbContext` so that the code generator can see through it: `opts.Services.AddDbContextWithWolverineIntegration<T>(...)`, or `AddDbContext<T>(..., optionsLifetime: ServiceLifetime.Singleton)`. Wolverine 6 forbids service location by default. A plain `AddDbContext<T>` and a lambda registration (`AddScoped<IFoo>(sp => ...)`) then throw `InvalidServiceLocationException` on the first message.
5. Use `AddScoped<IFoo, Foo>()` for services that handlers inject. Use `opts.CodeGeneration.AlwaysUseServiceLocationFor<T>()` only for one opaque type, with a reason comment. Do not call `opts.RestoreV5Defaults()` or relax `ServiceLocationPolicy` to make the host start.
6. A typed `HttpClient` (`AddHttpClient<IFoo, Foo>()`, with or without a configure lambda) is a factory registration too. A handler that injects `IFoo` throws `InvalidServiceLocationException` on its first message. Add `AlwaysUseServiceLocationFor<IFoo>()` with a reason, or inject `IHttpClientFactory` (it works). A test factory that replaces `IFoo` with a singleton fake hides the error.

### Handlers

7. Write a handler as a public class whose name ends in `Handler` or `Consumer`. Give it a `Handle`, `HandleAsync`, `Consume`, or `ConsumeAsync` method. Prefer static classes and methods. Do not use MassTransit, NServiceBus, or MediatR interfaces.
8. Return nothing, a record, a tuple, or `OutgoingMessages`. A method that returns a primitive (`int`, `bool`, `string`) is not a handler, and `InvokeAsync<bool>` then throws `IndeterminateRoutesException`.
9. Return cascading messages from the handler. Do not inject `IMessageBus` deep into domain services, and do not wrap Wolverine in an own `IMessagePublisher`. The generated code then commits the data and the outgoing messages together.

```csharp
public static class ShipOrderHandler
{
    // The returned message cascades: Wolverine sends it in the same transaction as the change.
    public static async Task<OrderShipped> Handle(ShipOrder command, OrdersDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.SingleAsync(o => o.Id == command.OrderId, ct);
        if (order.Cancelled)
        {
            throw new OrderRuleException($"Order {order.Id} is cancelled."); // policy: error queue at once
        }

        order.Shipped = true; // AutoApplyTransactions calls SaveChangesAsync
        return new OrderShipped(order.Id);
    }
}
```

10. Use `SendAsync` for a command that must have a handler: it throws when no route exists. `PublishAsync` is silent when nobody subscribes.

### Outbox and inbox

11. Configure all parts of the durable outbox (MSG-09):
    - `PersistMessagesWithPostgresql(...)` or `PersistMessagesWithSqlServer(...)`.
    - The EF Core registration of rule 4 and `UseEntityFrameworkCoreTransactions()`.
    - `Policies.AutoApplyTransactions()`.
    - `Policies.UseDurableOutboxOnAllSendingEndpoints()` and `Policies.UseDurableInboxOnAllListeners()`.
12. Outside a Wolverine handler (a minimal API endpoint, a `BackgroundService`), `AutoApplyTransactions` does nothing. Inject `IDbContextOutbox<T>`, call `outbox.PublishAsync(...)`, then `outbox.SaveChangesAndFlushMessagesAsync(ct)`. Or send a command with `bus.InvokeAsync(...)` to a handler. A `SaveChangesAsync` followed by `bus.PublishAsync` is two transactions.

```csharp
private static async Task<Created<OrderResponse>> CreateAsync(
    CreateOrderRequest request, IDbContextOutbox<OrdersDbContext> outbox, TimeProvider time, CancellationToken ct)
{
    var order = new Order(request.CustomerId!.Value, time.GetUtcNow());
    outbox.DbContext.Orders.Add(order);
    await outbox.PublishAsync(new OrderPlaced(order.Id, order.CustomerId, order.Total, order.PlacedAt));
    await outbox.SaveChangesAndFlushMessagesAsync(ct); // one transaction, then Wolverine sends
    return TypedResults.Created($"/orders/{order.Id}", order.ToResponse());
}
```

13. The durable inbox removes duplicates by envelope ID for `Durability.KeepAfterMessageHandling` (5 minutes by default). For business duplicates, use a natural key (MSG-10) or `DeliveryOptions.DeduplicationId`. Keep `Durability.InboxStaleTime` longer than the longest handler with its retries.

### RabbitMQ transport

14. Call `.UseQuorumQueues()` and `.ConfigureChannelCreation(o => { o.PublisherConfirmationsEnabled = true; o.PublisherConfirmationTrackingEnabled = true; })`. The transport declares classic queues and turns confirms off by default. Wolverine deletes the outbox row when it writes the frame. So the outbox alone does not protect against a broker refusal (Wolverine documentation).

    Use WolverineFx 6.51.3 or later with the RabbitMQ transport and the durable inbox. Versions before 6.51.3 can lose messages after a broker outage (issue GH-5123). On quorum queues that a durable inbox reads, add `.DeliveryLimitForDurableQuorumQueues(-1)` on the transport, or `.DeliveryLimit(-1)` on one listener. Each restart, reconnect, or shutdown returns the in-flight messages to the queue, and RabbitMQ 4 drops or dead-letters a message at its 21st return. The inbox owns the retries (MSG-08).

    `Inline` and buffered listeners keep the broker limit of 20. Wolverine sets the limit only on a queue that it declares. For an existing queue, set `delivery-limit` -1 in a broker policy instead (`dev-stack:rabbitmq`).
15. Declare the topology: `.AutoProvision()`, or exchanges and bindings that operations declares. Wolverine publishes with `mandatory: false`, so a message to an exchange without a binding is dropped. Mark a queue or exchange that another team owns `.ExternallyOwned()`. Else `AutoProvision()` declares it with Wolverine's own arguments, the broker refuses with `PRECONDITION_FAILED`, and Wolverine retries the broker start.
16. Choose the listener mode for each endpoint. The RabbitMQ listener default is `Inline` with prefetch 100. Use the durable inbox for business messages. `BufferedInMemory` acks at receipt and loses buffered messages in a crash. Use `ProcessInParallelWithNativeAcks()` for parallel work without a database, and `ListenerCount(n)` for parallel `Inline` work.

The full `Program.cs` is in `references/configuration.md`.

### Failures

17. Write an error policy for each exception type. Retry transient errors with cooldown, then a scheduled retry with jitter, then the error queue. Move validation and business errors to the error queue at once, or `Discard()` them on purpose (MSG-08). Without a policy, Wolverine runs a failing message 3 times.
18. Do not catch and log an exception in a handler. Wolverine then sees a success and acks the message.

### Contracts

19. Pin each contract name with `[MessageIdentity("orders.order-placed", Version = 1)]`. For a breaking change, add a new version type that implements `IForwardsTo<TNew>` on the old one, and register `opts.RegisterMessageForwarder<TOld, TNew>()`. Wolverine 6 no longer finds forwarders by itself (MSG-05).

### Tests

20. Test with tracked sessions: `host.TrackActivity().Timeout(30.Seconds()).InvokeMessageAndWaitAsync(message)`, then assert on `session.Sent`, `session.MovedToErrorQueue`, or `session.AllRecordsInOrder(...)`. Get the host with `factory.Services.GetRequiredService<IHost>()`. Do not use `Task.Delay`. A test of an error policy uses `SendMessageAndWaitAsync`: `InvokeMessageAndWaitAsync` runs inline, so it passes with any policy.
21. In tests, call `services.DisableAllExternalWolverineTransports()` and `services.RunWolverineInSoloMode()`. When `Program.cs` ends with `RunJasperFxCommands(args)`, set `JasperFxEnvironment.AutoStartHost = true` (namespace `JasperFx.CommandLine`) before you create the `WebApplicationFactory`. Without it, the factory fails with "The server has not been started". Detail: `references/testing.md`.

### Review

Wolverine 6 defects build and pass the tests, and show only in production. In a review, look for them across files:

22. For each new or changed handler, find the registration of each parameter in `Program.cs` (rules 4 to 6). Also read the test factory: a fake there hides the production registration.
23. When a handler moves to a new message type, search for the old type. Look for an `IForwardsTo<T>` without `RegisterMessageForwarder` (rule 19), and for tests that now send only the new type.
24. When production uses `TypeLoadMode.Static`, a change to a handler must also change `Internal/Generated/`. A deleted or stale folder passes the tests (Development uses `Dynamic`) and stops the production start (rule 2).

## Never write

| Wrong | Correct |
|-------|---------|
| `builder.Services.AddDbContext<OrdersDbContext>(o => ...)` with Wolverine handlers | `opts.Services.AddDbContextWithWolverineIntegration<OrdersDbContext>(o => ...)` |
| `public static bool Handle(CheckStock m)` and `InvokeAsync<bool>(...)` | `public static StockResult Handle(CheckStock m)` with `record StockResult(bool InStock)` |
| `class OrderPlacedConsumer : IConsumer<OrderPlaced>` with `ConsumeContext<T>` | `public static class OrderPlacedHandler { public static void Handle(OrderPlaced message, ...) }` |
| `await db.SaveChangesAsync(ct); await bus.PublishAsync(evt);` in an endpoint | `IDbContextOutbox<T>`: `PublishAsync`, then `SaveChangesAndFlushMessagesAsync(ct)` |
| `opts.UseRabbitMq(uri).AutoProvision();` and nothing else | `.UseQuorumQueues().DeliveryLimitForDurableQuorumQueues(-1).ConfigureChannelCreation(o => { both confirm flags }).AutoProvision()` |
| `x.Production.GeneratedCodeMode = TypeLoadMode.Dynamic` | `TypeLoadMode.Static` and `codegen write` in CI |
| A handler that injects `ICarrierClient` from `AddHttpClient<ICarrierClient, HttpCarrierClient>(...)` | `opts.CodeGeneration.AlwaysUseServiceLocationFor<ICarrierClient>()` with a reason |
| `opts.RestoreV5Defaults()` to make the host start | Fix the registrations (rule 4), or `AlwaysUseServiceLocationFor<T>()` for one type with a reason |
| `opts.OnException<Exception>().RetryWithCooldown(...)` for all errors | One policy per exception type. Business errors: `MoveToErrorQueue()` |
| `try { ... } catch (Exception ex) { logger.LogError(ex, ...); }` in a handler | Let the exception reach the error policy |
| `.UseNativeAck()` (does not exist) | `.ProcessInParallelWithNativeAcks()` |
| `opts.UseNewtonsoftForSerialization()` in new code | Keep System.Text.Json (the default) |
| `await Task.Delay(2000)` to wait for a handler | `host.TrackActivity().Timeout(...).InvokeMessageAndWaitAsync(...)` |

## Check

1. Build with the analyzers as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Start the host once: `dotnet run`, or a test with `WebApplicationFactory`. Pass: no `InvalidServiceLocationException`, no `IAssemblyGenerator` error. These errors come at startup or on the first message, not at build time.

3. In CI, check the generated code:

   ```
   dotnet run --project <app> -- codegen write
   git diff --exit-code -- '**/Internal/Generated/**'
   ```

   Pass: no diff.

4. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. Each `DbContext` that handlers inject is registered with the Wolverine integration or `optionsLifetime: Singleton`. Each typed `HttpClient` or factory registration that a handler injects has `AlwaysUseServiceLocationFor<T>()`. tool (WOLV-DBCONTEXT-OPTIONS, WOLV-TYPED-HTTPCLIENT), judge
2. No `RestoreV5Defaults()`. tool (WOLV-RESTORE-V5)
3. Endpoints and background services use `IDbContextOutbox<T>` or `InvokeAsync`. No `SaveChangesAsync` followed by `PublishAsync`. judge
4. The durable outbox has all its parts (rule 11). judge
5. The RabbitMQ transport has `UseQuorumQueues()` and both confirm flags, and the topology is declared. WolverineFx is 6.51.3 or later, and quorum queues of a durable inbox have delivery limit -1. Queues of another team are `ExternallyOwned()`. judge
6. Handlers return records, tuples, or `OutgoingMessages`, never a primitive. judge
7. Handlers do not catch and log exceptions. judge
8. Error policies exist for each exception type. Business errors go to the error queue at once. judge
9. No `IMessageBus` in domain services. Messages cascade from handlers. judge
10. No MassTransit, NServiceBus, or MediatR interfaces. tool (WOLV-FOREIGN-IDIOM)
11. Production uses `TypeLoadMode.Static`, CI runs `codegen write`, and `Internal/Generated/` changes with each changed handler. tool (WOLV-DYNAMIC-PROD), judge
12. Contracts have `[MessageIdentity]`. Each `IForwardsTo<T>` has `RegisterMessageForwarder`, and a test still sends the old version. judge
13. Tests use tracked sessions, not `Task.Delay`. Tests of error policies use `SendMessageAndWaitAsync`. tool (WOLV-TEST-DELAY), judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/messaging-principles.md`: read first. The principles that each message flow follows.
- `references/decisions.md`: read before new code needs MSG-02, MSG-04, MSG-08, MSG-09, or MSG-13.
- `references/configuration.md`: read when you write or review `UseWolverine`: the full `Program.cs`, the transport, the listener modes, and the durability settings.
- `references/handlers.md`: read when you write a handler, a cascading message, a scheduled message, or a message forwarder.
- `references/testing.md`: read before you write a test for a handler or a message flow.
- `references/wolverine5.md`: read only when the project uses Wolverine 5.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
