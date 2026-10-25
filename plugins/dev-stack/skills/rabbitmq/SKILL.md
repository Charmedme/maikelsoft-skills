---
name: rabbitmq
description: Messaging code on RabbitMQ 4.3 and 4.4 with RabbitMQ.Client 7, and the messaging principles that all message flows share (at-least-once delivery, idempotent consumers, outbox, contracts, names, dead letters). Use when code or a review touches a broker, an exchange, a queue, a routing key, a publisher or a consumer, IChannel, ConnectionFactory, BasicPublishAsync, BasicConsumeAsync, acks, prefetch, publisher confirms, quorum queues, dead-letter exchanges, an outbox table with a background publisher, EasyNetQ, or a RabbitMQ test container. Not for Wolverine handlers, its outbox, or its transport settings (dev-stack:wolverine), Kafka or Azure Service Bus, or broker users, vhosts and TLS (dev-stack:secure-coding).
metadata:
  verified: "2026-10-25"
  versions: "RabbitMQ 4.4.0 and 4.3.7, RabbitMQ.Client 7.2.2, EasyNetQ 8.1.11, Testcontainers.RabbitMq 4.16.0, .NET 10.0.13"
---

# RabbitMQ

The messaging principles, the RabbitMQ 4.3 and 4.4 broker, and the raw RabbitMQ.Client 7 API. Wolverine handlers and the Wolverine RabbitMQ transport are in `dev-stack:wolverine`. Broker users, vhosts, and TLS are in `dev-stack:secure-coding`. Test containers in general are in `dev-stack:dotnet-testing`.

## First

1. Read `references/stack-rules.md` and `references/messaging-principles.md`.
2. Find the versions:
   - Client: the `RabbitMQ.Client` or `EasyNetQ` package in `*.csproj` or `Directory.Packages.props`.
     - RabbitMQ.Client 7: use this file.
     - RabbitMQ.Client 6 (`IModel`, `CreateModel`): read `references/client.md`, section "Version 6 to 7". Do not upgrade unless the task asks for it.
     - EasyNetQ 8: read `references/easynetq.md`. Do not adopt EasyNetQ 9 (alpha).
   - Broker: the image tag in `compose*.yml`, the Helm values, or the test container. RabbitMQ 3.13 and older: read `references/broker.md`, section "From 3.13 to 4.x".
3. Check `docs/dev-team/decisions.md` for MSG-01 (in process or broker) and MSG-04 (library). When the service uses Wolverine, switch to `dev-stack:wolverine`.

Done when you can name the client version, the broker version, and the library decision.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| In process or broker | In process inside one deployable. A broker only between services, or for work that must survive a restart. | MSG-01 |
| Broker | RabbitMQ 4.4 with quorum queues for a new broker. An existing broker keeps its minor version. | MSG-03 |
| Library | Wolverine 6. Raw RabbitMQ.Client 7 only for interop or infrastructure code. EasyNetQ 8 only where it exists. | MSG-04 |
| Contracts | `<Service>.Contracts`, records, pinned wire names, additive changes, JSON sample tests. | MSG-05 |
| Names | Exchange `{publisher}.events` (topic). Routing key `{entity}.{event}.v{n}`. Queue `{consumer}.{purpose}`. Dead letters `{service}.dlq`. | MSG-06 |
| Queue type | Quorum, declared in code, plus `default_queue_type = quorum` on each vhost. | MSG-07 |
| Failures | Dead-letter policy on each work queue, `delivery-limit` 20, alert on dead-letter depth greater than 0. | MSG-08 |
| Outbox | Without Wolverine: own outbox table in the same `SaveChangesAsync`, background publisher with confirms. | MSG-09 |
| Idempotency | Natural key or upsert. Message ID table only for effects that cannot be idempotent. | MSG-10 |
| Wire format | System.Text.Json, `application/json`, camelCase, wire name in the AMQP `type` property. | MSG-11 |
| Tracing | Built-in ActivitySources `RabbitMQ.Client.*`, `MessageId` and `CorrelationId` on each message. | MSG-12 |

Existing code keeps its style (stack rule 3).

## Rules

### Delivery

1. Expect each message more than one time. Make each consumer idempotent with a unique business key or an upsert (MSG-10). The `redelivered` flag only says "maybe".
2. A database write and a publish must both happen or both fail. Write an outbox row in the same `SaveChangesAsync`, and publish from a `BackgroundService` (MSG-09). No transaction spans the database and the broker. A publish after `SaveChangesAsync` loses the event when the broker is down or the process stops. The code is in `references/outbox.md`.

### Topology

3. Declare each durable work queue with `x-queue-type: quorum`, and set `default_queue_type = quorum` on the vhost as a safety net (MSG-07). A classic queue lives on one node, so it does not survive the loss of that node.
4. Do not send `x-queue-mode`, `x-queue-version`, `x-queue-master-locator`, or `ha-*` policies. Do not declare a queue that is not durable and not exclusive. RabbitMQ 4.x removed these features and rejects the declaration.
5. Put dead-lettering, `delivery-limit`, and length limits in a policy when operations owns the broker. Queue arguments cannot change after the declaration, and a second declaration with other arguments fails with `PRECONDITION_FAILED`. Detail: `references/broker.md`.

### Publishing

6. Create the publish channel with `new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)`, await each publish, and catch `PublishReturnException` and `PublishException`. Without confirms, a publish during a connection failure is lost without an error.
7. Publish with `mandatory: true` when an unroutable message is a bug. Without it, the broker drops a message that no queue binds.
8. Keep one long-lived `IConnection` per process role, with `ClientProvidedName`. Give each concurrent publisher its own `IChannel`. A shared channel mixes frames of concurrent publishes.
9. Set `Persistent`, `MessageId`, `CorrelationId`, `ContentType`, `Type` (the pinned wire name), and `Timestamp` on each message (MSG-11, MSG-12). A consumer in another language reads the type and the ID from these properties.

```csharp
await using var channel = await connection.CreateChannelAsync(
    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), ct);

var properties = new BasicProperties
{
    Persistent = true,
    MessageId = messageId.ToString(),
    CorrelationId = correlationId,
    ContentType = "application/json",
    Type = "order.placed.v1", // the pinned wire name, not the CLR type name
    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
};
var body = JsonSerializer.SerializeToUtf8Bytes(evt, s_json); // JsonSerializerDefaults.Web: camelCase

// With confirmation tracking, the await ends when the broker confirms the message.
// It throws PublishException on a nack and PublishReturnException when no queue binds the key.
await channel.BasicPublishAsync("orders.events", "order.placed.v1", mandatory: true, properties, body, ct);
```

### Consuming

10. Consume with `autoAck: false`. Ack after the work and the database commit. An early ack turns a crash into data loss.
11. Set a prefetch per consumer: `BasicQosAsync(0, n, global: false)`. Start between 10 and 100 and measure. Without a `BasicQosAsync` call, the prefetch is 0 (no limit), and one consumer can hold the full backlog unacked. Global QoS is a deprecated feature (`global_qos`), and quorum queues do not support it. Do not depend on an error: on 4.3.6 the call with `global: true` did not fail.
12. Deserialize or copy `ea.Body` inside `ReceivedAsync`. The client reuses that memory after the handler returns.
13. Classify failures (principle 7). Permanent (validation, unknown type): `BasicNackAsync(requeue: false)`, so that the dead-letter exchange takes it. Transient: retry with a delay (DLX plus TTL, or the quorum delayed retry of 4.3). Keep `delivery-limit` at 20. On 4.3 and later, `BasicNackAsync(requeue: true)` does not count toward it: requeue with `BasicRejectAsync(requeue: true)`. Use `dead-letter-strategy=at-least-once` with `overflow=reject-publish` for a queue whose dead letters must not get lost (MSG-08).
14. Expect no global order. For order with failover, use `x-single-active-consumer`. Raise `ConsumerDispatchConcurrency` above 1 (the default) only when order does not matter. Keep a handler shorter than the consumer timeout (30 minutes, enforced on quorum queues). On 4.3 and later, the broker then cancels the consumer. RabbitMQ.Client 7 does not consume again by itself: handle `UnregisteredAsync`. The timeout does not count toward the `delivery-limit`. Send a reference instead of a body above 16 MiB.
15. Do not wait with `Task.Delay` inside `ReceivedAsync` before a retry. With `ConsumerDispatchConcurrency` 1, the client awaits each handler, so the wait stops all other deliveries of the channel. Let the broker delay the message (rule 13, `references/broker.md`, section "Delayed retry").

The full consumer and the topology code are in `references/client.md`.

### Existing consumers

16. Before a new consumer uses an existing consumer base class, check it for these defects. Each one loses or blocks messages without an error:
    - `BasicNackAsync(requeue: true)` for each error: on 4.3 and later, a failing message loops forever (rule 13).
    - No `BasicQosAsync`: prefetch 0 (rule 11).
    - Deserialization outside the `try`: when the JSON is not valid, the exception leaves `ReceivedAsync`, and the message stays unacked until the channel closes.
    - Default `JsonSerializerOptions` (case-sensitive): a camelCase producer gives empty fields, without an error. `JsonSerializerDefaults.Web` reads both.
    - A domain rule violation (for example an `ArgumentException` of the entity) treated as transient: the message waits through 20 retries before the dead-letter queue. It is permanent.
    - Shared queue arguments that the change extends: the redeclaration of an existing queue fails with `PRECONDITION_FAILED` (rule 5).

### EasyNetQ

17. EasyNetQ 8: register with `services.AddEasyNetQ("host=...;publisherConfirms=true;consumerDispatcherConcurrency=1")`. Set both values. By default, the dispatch concurrency follows `prefetchCount` (50). Then 50 messages run in parallel and out of order. Use `WithQueueType(QueueType.Quorum)`. EasyNetQ has no built-in retry. Detail: `references/easynetq.md`.

### Tests

18. Test against a real broker of the production minor version, for example `new RabbitMqBuilder("rabbitmq:4.3-management-alpine")`. Wait for a message with a `TaskCompletionSource` and a timeout, not with `Task.Delay`. Do not mock `IChannel`. Test the broker-down path: stop the container, act, start it again, and assert that the event arrives. Detail: `references/testing.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `IModel`, `CreateModel()`, `CreateBasicProperties()`, `EventingBasicConsumer`, `ConfirmSelect()` | `IChannel`, `CreateChannelAsync(options)`, `new BasicProperties { ... }`, `AsyncEventingBasicConsumer`, `CreateChannelOptions(true, true)` |
| `factory.CreateConnection()` | `await factory.CreateConnectionAsync(ct)` |
| `services.AddSingleton(await connection.CreateChannelAsync())` for all publishers | One channel per publisher, or the outbox publisher that owns its channel |
| `_ = channel.BasicPublishAsync(...)` | `await channel.BasicPublishAsync(...)` on a channel with confirms |
| `await db.SaveChangesAsync(ct); await channel.BasicPublishAsync(...);` | An outbox row in the same `SaveChangesAsync` and a background publisher |
| `["x-queue-mode"] = "lazy"`, `ha-mode` policy | `["x-queue-type"] = "quorum"` |
| `BasicQosAsync(0, 20, global: true)` | `BasicQosAsync(0, 20, global: false)` |
| `BasicConsumeAsync(queue, autoAck: true, ...)` | `autoAck: false` and `BasicAckAsync` after the commit |
| `catch { await ch.BasicNackAsync(tag, false, requeue: true); }` for each error | Classify. `requeue: false` for permanent errors, plus a dead-letter policy. On 4.3 and later this nack never reaches the `delivery-limit` |
| `RabbitHutch.CreateBus("host=...")` | `services.AddEasyNetQ("host=...;publisherConfirms=true")` |
| `UseDelayedExchangeScheduler()`, `"x-delayed-type"` | The default DLX plus TTL scheduler, or the quorum delayed retry |
| `new RabbitMqBuilder().Build()` | `new RabbitMqBuilder("rabbitmq:4.3-management-alpine").Build()` |

## Check

1. Build with the analyzers as errors. The compiler rejects the 6.x API. CS0618 flags the old Testcontainers constructor. CA2012 flags a `ValueTask` that is not awaited.

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Check the packages:

   ```
   dotnet list package --vulnerable --include-transitive
   ```

   Pass: no vulnerable package.

3. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

4. On a running broker:

   ```
   rabbitmqctl list_queues name type durable arguments
   ```

   Pass: each work queue has the type `quorum`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. No `x-queue-mode`, `x-queue-version`, or `ha-*` policy. tool (RMQ-REMOVED-ARGS)
2. Each durable work queue is a quorum queue. tool (`rabbitmqctl`), judge
3. No global QoS. tool (RMQ-GLOBAL-QOS)
4. Consumers use `autoAck: false` and ack after the commit. tool (RMQ-AUTOACK), judge
5. A database write and a publish go through an outbox. No publish after `SaveChangesAsync`. judge
6. The publish channel has confirms and tracking. Each publish is awaited, and a failure is not swallowed. tool (CA2012), judge
7. No channel shared by concurrent publishers. tool (RMQ-SINGLETON-CHANNEL), judge
8. Each message has `Persistent`, `MessageId`, `CorrelationId`, `ContentType`, `Type`, and `Timestamp`. judge
9. `ea.Body` is read inside the handler. judge
10. Permanent errors go to a dead-letter exchange. No `requeue: true` for each error. Each work queue has a dead-letter policy and a delivery limit. judge
11. Consumers are idempotent on a business key. judge
12. Contracts have pinned wire names and change only by addition. judge
13. Tests use a pinned 4.x broker image and wait with a signal, not `Task.Delay`. tool (RMQ-TC-OLD-IMAGE, CS0618), judge
14. No delayed message exchange plugin. tool (RMQ-DELAYED-PLUGIN)
15. No `BasicNackAsync` with `requeue: true`: on 4.3 and later it does not count toward the `delivery-limit`. tool (RMQ-NACK-REQUEUE)
16. The existing consumer code that the change uses has none of the defects of rule 16. judge

Do not flag these correct patterns (detail in `references/review-checklist.md`):

- `ea.Body.ToArray()` before the first `await`.
- `BasicNackAsync(requeue: false)` for JSON that is not valid.
- One channel that a `SemaphoreSlim` guards.
- A unique index on a nullable business key on PostgreSQL.

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

In the open points, name the owner of each dead-letter queue (this service, or a named team) and the replay step.

## References

- `references/messaging-principles.md`: read first. The principles that each message flow follows.
- `references/decisions.md`: read before new code needs an MSG decision. It has the question, the options, and the recommendation.
- `references/client.md`: read when you write a connection, a channel, a publisher, or a consumer, or move code from version 6 to 7.
- `references/outbox.md`: read when a database write and a publish must both happen in a service without Wolverine.
- `references/broker.md`: read when you declare a queue, an exchange, a policy, or a dead-letter route, or upgrade the broker from 3.x.
- `references/easynetq.md`: read only when the project uses EasyNetQ.
- `references/testing.md`: read before you write a test against the broker.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
