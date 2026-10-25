# RabbitMQ 4.3 and 4.4 broker

The broker side: queue types, policies, dead letters, retries, limits, and the changes from 3.13 to 4.x and from 4.3 to 4.4. Facts marked "docs" come from the RabbitMQ documentation and release notes (checked on 2026-10-16, 2026-10-19, and 2026-10-25). They were not run on a broker for this skill, except where the line says "run".

## Contents

- Queue types
- Arguments or policies
- Dead letters
- Delayed retry
- Limits and timeouts
- Order
- From 3.13 to 4.x
- From 4.3 to 4.4
- Commands

## Queue types

| Type | Use | Notes |
|------|-----|-------|
| Quorum | Each durable work queue | Replicated with Raft. Poison message handling with `delivery-limit`. Durable only. |
| Classic | Exclusive, auto-delete reply queues | One node. No mirroring in 4.x. |
| Stream | Replay, large fan-out, an event log | Consumers read with an offset. Not a work queue. |

- Declare the type in code: `["x-queue-type"] = "quorum"`. A queue that the app declared as classic stays classic: the type cannot change. To move a queue, declare a new queue, move the consumers, and delete the old queue.
- Set `default_queue_type = quorum` on each vhost (`rabbitmqctl update_vhost_metadata <vhost> --default-queue-type quorum`). Then a declaration without `x-queue-type` from another client also gets a quorum queue.
- A quorum queue on a single-node test broker works. It has one member. (run)

## Arguments or policies

Queue arguments are fixed at the declaration. A second declaration with other arguments fails with `PRECONDITION_FAILED` and closes the channel. Policies change at run time and apply to queues by a name pattern.

- Put in the declaration: `x-queue-type`, and what the code depends on (`x-single-active-consumer`).
- Put in a policy: `dead-letter-exchange`, `delivery-limit`, `max-length`, `overflow`, `message-ttl`, `dead-letter-strategy`. Operations can then change them without a deploy.

```
rabbitmqctl set_policy billing-work "^billing\." \
  '{"dead-letter-exchange":"billing.dlx","delivery-limit":20}' \
  --apply-to quorum_queues --priority 10
```

- On 4.3 and older, a quorum queue reads `delivery-limit` from a policy only when the queue starts. A later change of the policy does not change the limit of an existing queue. From 4.4, the queue takes the new value, and the default of 20 comes back when the policy is removed. (docs, 4.4.0 release notes)

When nobody runs policies (a small service, a test), arguments in the declaration are acceptable. Do not mix both for the same key.

## Dead letters

- Give each service one dead-letter exchange and one dead-letter queue `{service}.dlq` (MSG-06). A dead-letter queue per listener only when another team owns the queue.
- A message goes to the dead-letter exchange on `BasicNack` or `BasicReject` with `requeue: false`, on an expired TTL, on a length overflow, and when it passes the `delivery-limit` of a quorum queue.
- `delivery-limit` on quorum queues: keep 20 (the 4.x default). A requeue loop then ends in the dead-letter queue, not in a hot loop forever. From 4.3, the limit counts only failed deliveries (`delivery-count`): `basic.reject`, and a closed channel or connection. A `basic.nack` with requeue only increments `acquired-count`, and does not count.
- Quorum queues dead-letter with `at-most-once` by default: a message can get lost on the way to the dead-letter queue. For a queue whose dead letters must not get lost, set `dead-letter-strategy=at-least-once` and `overflow=reject-publish` in the policy. (docs)
- Alert on a dead-letter depth greater than 0. Each dead-letter queue has an owner and a replay procedure: read, fix the cause, and publish again to the original exchange with a new message ID.

## Delayed retry

Options for a retry with a delay on the raw client:

1. Dead-letter exchange plus a TTL queue: nack with `requeue: false` to a retry exchange. A retry queue with `x-message-ttl` dead-letters the message back to the work exchange after the delay. One retry queue for each delay step. Works on each 4.x version.
2. Quorum delayed retry (RabbitMQ 4.3 and later): the broker delays a returned message itself, with `delay = min(min * delivery-count, max)` in milliseconds. Set `x-delayed-retry-type`, `x-delayed-retry-min`, and `x-delayed-retry-max` (or the policy keys `delayed-retry-type`, `delayed-retry-min`, `delayed-retry-max`). The type `failed` delays a `basic.reject`, `returned` delays a `basic.nack`, and `all` delays both. The default is `disabled`. Documentation only: it was not tested for this skill. Use it only after a test on the broker version of production. (docs)
   With it, the consumer calls `BasicRejectAsync(requeue: true)` and returns at once. The broker waits, not the handler, so other deliveries of the channel continue.
3. The delayed message exchange plugin (`x-delayed-message`): not a default. It keeps messages on one node and is not replicated.
4. From 4.4, quorum queues hold a message with the header `x-opt-delivery-time` until that time. The value is a `long` in milliseconds since the Unix epoch. A publisher can use it for a delay that it sets itself. Documentation only. (docs, 4.4.0 release notes)

Wolverine and EasyNetQ have their own scheduling (`dev-stack:wolverine`, `easynetq.md`).

## Limits and timeouts

- Consumer timeout: 30 minutes by default. From 4.3, the quorum queue enforces it itself. On a timeout, the broker returns the message to the queue and sends `basic.cancel` to a client with the `consumer_cancel_notify` capability (RabbitMQ.Client 7 has it). Other clients lose the channel. From 4.3.7, `consumer_timeout_response = close_channel` in `rabbitmq.conf` restores the old behavior. (docs)
- RabbitMQ.Client 7 does not register a cancelled consumer again: it calls `HandleBasicCancelAsync`, and `AsyncEventingBasicConsumer` raises `UnregisteredAsync`. Without code that consumes again, the consumer stops and the queue grows. Automatic recovery helps only after a connection failure.
- A consumer timeout does not count toward the `delivery-limit` (it increments only `acquired-count`). A handler that always takes too long loops. Keep handlers short, or move long work to a job with its own state.
- Message size: the default maximum is 16 MiB. Send a reference (a blob URL) for larger content.
- Global QoS (`global: true`) is not supported on quorum queues. The deprecated feature `global_qos` is denied by default in 4.3. But on 4.3.6, `BasicQosAsync(0, 10, true)` did not fail, and the consumer got a prefetch of 10. Use `global: false`.
- Use a prefetch per consumer between 10 and 100. Measure. A large prefetch holds many unacked messages in one consumer.

## Order

- One queue with one consumer and prefetch 1 keeps the order, but a retry or a requeue changes it.
- `x-single-active-consumer: true`: only one consumer receives at a time. Another consumer takes over on failure. Use it when order matters and you need failover.
- A consistent hash exchange or a stream with partitions spreads load while each key keeps its order. That is an advanced setup: ask the human.

## From 3.13 to 4.x

| 3.13 and older | 4.x |
|----------------|-----|
| Classic mirrored queues (`ha-mode`, `ha-params`, `ha-sync-mode` policies) | Removed. Use quorum queues. |
| `x-queue-mode: lazy` | Removed. Classic queues v2 always store on disk. The declaration fails. |
| `x-queue-version`, `x-queue-master-locator` | Removed. |
| A queue that is not durable and not exclusive (transient) | Rejected. |
| `delivery-limit` unset (unlimited) on quorum queues | Default 20. |
| Global QoS allowed | Denied by default in 4.3 (a deprecated feature). |
| AMQP 1.0 as a plugin | Built in. |

Check the deprecated features of the target version before an upgrade: `rabbitmqctl list_deprecated_features --used`.

## From 4.3 to 4.4

RabbitMQ 4.4.0 came out on 2026-10-20 (community support to 2027-07-31). 4.3 has community support to 2027-01-31. (docs)

- No removed feature for an AMQP 0-9-1 app. The rules of this skill for 4.3 stay the same: `basic.nack` with requeue still does not count toward the `delivery-limit`.
- `delivery-limit` in a policy now changes existing quorum queues (section "Arguments or policies").
- A passive declaration needs one of the `configure`, `write`, or `read` permissions on the queue or exchange.
- `GET /api/auth/hash_password/{password}` is replaced by `POST /api/auth/hash_password`.
- A node with classic queue files in the new v2 format cannot go back to 4.3.
- When you upgrade Erlang to 28 or later at the same time, upgrade to 4.3.7 or later first.

## Commands

```
rabbitmqctl list_queues name type durable arguments messages consumers
rabbitmqctl list_policies
rabbitmqctl list_deprecated_features --used
rabbitmq-diagnostics check_port_connectivity
```
