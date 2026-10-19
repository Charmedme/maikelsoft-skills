# Messaging principles

The source of this file is `plugins/dev-stack/shared/messaging-principles.md` in the repository github.com/Charmedme/maikelsoft-skills. The skills `rabbitmq` and `wolverine` have a copy. To change the principles, edit the source and run `python3 scripts/sync_shared.py`.

These principles apply to each message flow: a broker queue, a Wolverine local durable queue, or an outbox table with a background sender. The skill adds the rules of its library.

## Contents

- 1. In process first
- 2. At-least-once delivery
- 3. Idempotent consumers
- 4. Outbox for a write and a send
- 5. Contracts
- 6. Names
- 7. Failures and dead letters
- 8. Order
- 9. Identity and tracing
- 10. Tests

## 1. In process first

Inside one deployable, call the code directly, or raise a domain event in the same transaction (MSG-01). Use a broker only between services, or for work that must survive a restart. A broker adds a network hop, a second store, and a new failure mode.

## 2. At-least-once delivery

Each real broker and each durable queue delivers a message one or more times. Exactly-once delivery does not exist across a network. A consumer can crash after the work and before the ack, so the broker sends the message again. The `redelivered` flag only says "maybe seen before". Design each consumer for duplicates.

A message can also come late. A consumer that reads a newer state in the database than the message describes must not go back to the old state.

## 3. Idempotent consumers

Make the effect idempotent with a business key (MSG-10):

- A unique index on the business key, and an insert that treats the unique violation as "already done".
- An upsert with a version check: apply the change only when the stored version is lower than the version in the message.
- A state check: "cancel" on an order that is already cancelled does nothing.

Keep a table of handled message IDs only for an effect that cannot be idempotent, for example a call to an external payment API. Write the ID in the same transaction as the effect, or the table does not help.

## 4. Outbox for a write and a send

No transaction spans a database and a broker. When a database change and a message must both happen, write the message as an outbox row in the same transaction as the change (MSG-09). A background sender reads the rows, sends each one, and marks it sent only after the broker confirms it.

The other orders lose data:

| Order | What goes wrong |
|-------|-----------------|
| Save, then send | The process stops or the broker is down after the save: the change exists and the message is lost. |
| Send, then save | The save fails: the consumer acts on a change that does not exist. |
| Send inside the open transaction | The transaction rolls back after the send: the same problem. |

The outbox sends each message at least one time (principle 2). The consumer handles the duplicates (principle 3).

## 5. Contracts

- Put the message types of a publishing service in one `<Service>.Contracts` project with records only and no dependencies (MSG-05). Consumers in other languages get a JSON sample, not the C# type.
- Give each message a stable wire name that does not change when a class or namespace moves. Do not use the CLR type name on the wire.
- Change a contract only by adding optional fields. For a breaking change, make a new version (`order.placed.v2`), publish both for a time, and remove the old one when no consumer reads it.
- Keep a JSON sample of each contract in the tests. A test serializes the record and compares the result with the sample. Then a rename breaks the build, not the consumer.
- Send System.Text.Json with camelCase and `application/json` (MSG-11). Send instants as ISO 8601 with offset, money as a number with a currency field, enums as strings.

## 6. Names

Use lowercase, dot-separated names, with kebab-case words (MSG-06):

| Item | Pattern | Example |
|------|---------|---------|
| Event exchange | `{publisher}.events` (topic) | `orders.events` |
| Command exchange or queue | `{service}.commands` | `billing.commands` |
| Routing key or wire name | `{entity}.{event}.v{n}` | `order.placed.v1` |
| Consumer queue | `{consumer}.{handler-or-purpose}` | `billing.order-placed` |
| Dead-letter queue | `{service}.dlq`, one per service | `billing.dlq` |

A consumer owns its queue and its binding. The publisher owns the exchange and the contract. A dead-letter queue per listener is only for a queue that another team owns.

## 7. Failures and dead letters

Classify each failure before you retry it (MSG-08):

| Class | Examples | Action |
|-------|----------|--------|
| Transient | Timeout, lost connection, deadlock, HTTP 503 | Retry with a growing delay and jitter. Then the dead-letter queue. |
| Permanent | Validation error, unknown message type, a business rule says no | The dead-letter queue at once, or discard on purpose. No retry. |
| Poison | A message that crashes the consumer each time | A delivery limit moves it to the dead-letter queue. |

- A retry of a permanent error only delays the dead letter and uses capacity.
- A requeue without a delay is a hot loop.
- Do not catch and log an exception, then ack. The message is then lost without a trace.
- Each dead-letter queue has an owner, an alert on depth greater than 0, and a replay procedure.

## 8. Order

Expect no global order. Retries, more than one consumer, and prefetch change the order. When order matters for one entity, use a version number in the message and ignore older versions, or use one active consumer for the queue. Do not run a whole queue on one consumer for order: one slow message then blocks all others.

## 9. Identity and tracing

Give each message a `MessageId` (unique for each send) and a `CorrelationId` (the same for each message of one business flow) (MSG-12). Log both. Use the built-in OpenTelemetry sources of the library, so that a trace goes from the HTTP request through the broker to the consumer.

## 10. Tests

- Test a handler as a plain method when you can.
- Test the flow against a real broker or the real durable queue of the production major version. A mock of the client proves nothing about acks, confirms, or redelivery.
- Wait for a message with a completion signal and a timeout, not with a fixed delay. A fixed delay is slow when it is long and flaky when it is short.
- Test the failure path too: the broker down, a duplicate message, a permanent error.
