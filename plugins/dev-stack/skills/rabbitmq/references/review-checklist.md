# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Delivery and outbox
- Topology
- Publishing
- Consuming and failures
- Contracts and tracing
- Tests
- Decisions
- Correct patterns that are not defects

## Delivery and outbox

| # | Check | Type |
|---|-------|------|
| 1 | A database write and a publish go through an outbox row in the same `SaveChangesAsync`. No publish after `SaveChangesAsync`, no publish inside the open transaction. | judge |
| 2 | The outbox publisher marks a row sent only after the confirm, and keeps the row on a failure. | judge |
| 3 | The app starts and accepts requests when the broker is down (lazy connection in a background service). | judge |
| 4 | Each consumer is idempotent on a business key (unique index, upsert with version, state check). | judge |

## Topology

| # | Check | Type |
|---|-------|------|
| 5 | No `x-queue-mode`, `x-queue-version`, `x-queue-master-locator`, or `ha-*` policy. | tool (RMQ-REMOVED-ARGS) |
| 6 | Each durable work queue is a quorum queue. No transient (not durable, not exclusive) queue. | tool (`rabbitmqctl list_queues`), judge |
| 7 | Each work queue has a dead-letter exchange and a delivery limit, in a policy or in the declaration. | judge |
| 8 | Names follow MSG-06. The consumer declares its queue and binding. | judge |
| 9 | No delayed message exchange plugin. | tool (RMQ-DELAYED-PLUGIN) |

## Publishing

| # | Check | Type |
|---|-------|------|
| 10 | The publish channel has `publisherConfirmationsEnabled` and `publisherConfirmationTrackingEnabled`. | judge |
| 11 | Each `BasicPublishAsync` is awaited. `PublishException` and `PublishReturnException` are not swallowed. | tool (CA2012), judge |
| 12 | No channel shared by concurrent publishers. No `IChannel` singleton for all requests. | tool (RMQ-SINGLETON-CHANNEL), judge |
| 13 | One long-lived connection per process role, with `ClientProvidedName`. | judge |
| 14 | `mandatory: true` when an unroutable message is a bug. | judge |
| 15 | Each message has `Persistent`, `MessageId`, `CorrelationId`, `ContentType`, `Type`, and `Timestamp`. | judge |

## Consuming and failures

| # | Check | Type |
|---|-------|------|
| 16 | `autoAck: false`. Ack after the work and the commit. | tool (RMQ-AUTOACK), judge |
| 17 | Prefetch per consumer (`global: false`), between 10 and 100. | tool (RMQ-GLOBAL-QOS), judge |
| 18 | `ea.Body` is read or copied inside `ReceivedAsync`. | judge |
| 19 | Failures are classified: permanent errors with `requeue: false`, transient errors with a delayed retry. No catch-all with `requeue: true`. No `BasicNackAsync` with `requeue: true` (on 4.3 and later it does not count toward the `delivery-limit`). A domain rule violation is permanent. No `Task.Delay` in the handler before a retry. | tool (RMQ-NACK-REQUEUE), judge |
| 20 | Each path of `ReceivedAsync` acks or nacks. No exception leaves the handler. | judge |
| 21 | `ConsumerDispatchConcurrency` above 1 only where order does not matter. | judge |
| 22 | Handlers end well inside the 30-minute consumer timeout. The consumer consumes again after the broker cancels it (`UnregisteredAsync`). | judge |
| 22a | The existing consumer base class has none of the defects of `SKILL.md` rule 16. | judge |

## Contracts and tracing

| # | Check | Type |
|---|-------|------|
| 23 | Contracts are records with a pinned wire name. A change only adds optional fields. A breaking change is a new version. | judge |
| 24 | JSON is camelCase (`JsonSerializerDefaults.Web`) with `application/json`. A contract sample test exists. | judge |
| 25 | Tracing adds `RabbitMQ.Client.*` sources. No prerelease package without approval. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 26 | Tests use a pinned 4.x image, not `new RabbitMqBuilder()`. | tool (RMQ-TC-OLD-IMAGE, CS0618) |
| 27 | Tests wait with a `TaskCompletionSource` and a timeout, not with `Task.Delay`. | judge |
| 28 | No mock of `IChannel` or `IConnection`. | judge |
| 29 | A test covers the broker-down path or a duplicate message. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 30 | New code follows the accepted MSG decisions, or the report marks them as assumed. | judge |
| 31 | EasyNetQ only where it exists (MSG-04). No EasyNetQ 9 alpha. | tool (RMQ-PKG-EASYNETQ-ALPHA), judge |

## Correct patterns that are not defects

Do not report these as findings:

- `ea.Body.ToArray()` before the first `await` in `ReceivedAsync`. It copies the body while the memory is valid. The client reuses the memory only after the handler returns.
- `BasicNackAsync(requeue: false)` for a `JsonException`. JSON that is not valid never succeeds, and the nack sends the message to the dead-letter exchange at once. Only a nack with `requeue: true` is wrong.
- One channel in a singleton publisher that a `SemaphoreSlim` guards. Two publishes never use it at the same time. RMQ-SINGLETON-CHANNEL is about a channel that concurrent publishers share without a guard.
- A state check that returns without an error when the effect is already done (for example `MarkPaid` on a paid order). That is the idempotent consumer of principle 3.
- A unique index on a nullable business key on PostgreSQL. PostgreSQL treats NULL values as distinct in a unique index (unless the index has `NULLS NOT DISTINCT`), so old rows without a value do not conflict. On SQL Server, a unique index allows only one NULL: there the index needs the filter `IS NOT NULL` (EF Core adds it for a nullable column).
