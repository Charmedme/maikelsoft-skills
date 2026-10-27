# Wolverine decisions

The team decisions that Wolverine code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). MSG-01, MSG-03, MSG-05, MSG-06, MSG-07, MSG-10, MSG-11, and MSG-12 are in `dev-stack:rabbitmq`, because they apply to each message flow. DATA-06 (unit of work) is in `dev-stack:efcore`.

## Contents

- MSG-04. Messaging library
- MSG-02. In-process dispatch library
- MSG-09. Outbox and inbox
- MSG-08. Retry and dead-letter policy
- MSG-13. Wolverine code generation

## MSG-04. Messaging library

- Question: Which library does the service use for messaging: Wolverine, raw RabbitMQ.Client, or EasyNetQ?
- Ask when: the first publisher or consumer in the service.
- Options: Wolverine 6 (MIT): handlers, durable outbox and inbox on EF Core, retries, RabbitMQ transport, scheduled messages. Raw RabbitMQ.Client 7: full control, own outbox, own retry. EasyNetQ 8: simple publish and subscribe, no outbox. MassTransit 9: commercial license.
- Recommended: Wolverine 6 for a service that writes to a database and publishes (the common case). It gives the outbox, the inbox, and the retry policies in one library.
- Choose another option when: the service runs on .NET 8 (Wolverine 6 needs .NET 9 or 10). The service only bridges to a system in another language: raw client (`dev-stack:rabbitmq`). The team already runs MassTransit with a license.
- Depends on: MSG-01, MSG-03.
- Revisit when: Wolverine 7 ships. The license of Wolverine changes.
- Check: judge.
- Detect: DET-MSG-04.

## MSG-02. In-process dispatch library

- Question: Does the service use a mediator for in-process calls?
- Ask when: the first handler class, or a request to add MediatR.
- Options: None: inject the handler service and call it. Wolverine `InvokeAsync`: when Wolverine is already in the service. A source-generated mediator (Mediator by martinothamar, MIT). MediatR: not allowed (standing rule).
- Recommended: None. Inject handler services directly. Use Wolverine `InvokeAsync` only when the service already uses Wolverine, so that the call gets the transaction and the middleware.
- Choose another option when: the team wants pipeline behaviors for each call and does not use Wolverine: a source-generated mediator.
- Depends on: MSG-04.
- Revisit when: cross-cutting code repeats in many handlers.
- Check: judge.
- Detect: none.

## MSG-09. Outbox and inbox

- Question: How does the service make a database write and a message happen together?
- Ask when: the first message that follows a database write.
- Options: Wolverine durable outbox on all sending endpoints and durable inbox on business listeners. Durable outbox only. Buffered (in memory) endpoints: messages are lost on a crash.
- Recommended: `PersistMessagesWithPostgresql` (or SQL Server) on the service database, `AutoApplyTransactions()` with the EF Core integration, `UseDurableOutboxOnAllSendingEndpoints()`, and `UseDurableInboxOnAllListeners()`. Endpoints and background services use `IDbContextOutbox<T>` or `InvokeAsync`.
- Choose another option when: a listener only refreshes a cache and needs no database: `ProcessInParallelWithNativeAcks()` without the inbox.
- Depends on: MSG-04, DATA-01.
- Revisit when: the message tables grow: check `KeepAfterMessageHandling` and the cleanup.
- Check: judge.
- Detect: DET-MSG-09.

## MSG-08. Retry and dead-letter policy

- Question: How do handlers retry, and where do failed messages go?
- Ask when: the first handler that can fail.
- Options: The catalog chain below. Retries only. The Wolverine default (3 attempts, then the error queue).
- Recommended: For transient exceptions, use 3 immediate retries with cooldown (50 ms, 250 ms, 1 s). Then use 2 scheduled retries with jitter (10 s, 1 min), then the error queue. Validation and business errors go to the error queue at once. One dead-letter queue per service (`{service}.dlq`, `CustomizeDeadLetterQueueing`). On the broker, a quorum queue that a durable inbox reads has `delivery-limit` -1 (`DeliveryLimitForDurableQuorumQueues(-1)`). Other work queues keep the limit of 20 (`dev-stack:rabbitmq`).
- Choose another option when: a message has no value after a short time: `Discard()` on purpose, with a log line.
- Depends on: MSG-04, MSG-06.
- Revisit when: the dead-letter queue fills in normal operation.
- Check: judge (a tracked session test of the business error).
- Detect: none.

## MSG-13. Wolverine code generation

- Question: How does Wolverine get its generated handler code in production?
- Ask when: the first deployment of a service with Wolverine.
- Options: Static in production with `codegen write` in CI and the generated code in git. Dynamic everywhere with `WolverineFx.RuntimeCompilation`. Auto (generate at the first start and write files).
- Recommended: Static in production through `CritterStackDefaults`, Dynamic in development, and the `WolverineFx.RuntimeCompilation` package in all builds. Static starts fast and fails at once when a generated type is missing. A conditional package reference saves little and adds moving parts.
- Choose another option when: the service is small and start time does not matter: Dynamic everywhere.
- Depends on: MSG-04.
- Revisit when: start time or memory in production is a problem, or the team forgets `codegen write`.
- Check: CI runs `codegen write` and `git diff --exit-code` on `Internal/Generated/`.
- Detect: DET-MSG-13.
