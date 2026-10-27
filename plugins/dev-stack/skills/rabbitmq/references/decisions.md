# Messaging decisions

The team decisions that messaging code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4). MSG-02 (in-process dispatch) and MSG-13 (Wolverine code generation) are in `dev-stack:wolverine`.

## Contents

- MSG-01. In-process events or a broker
- MSG-03. Broker choice
- MSG-04. Messaging library
- MSG-05. Message contract location and versioning
- MSG-06. Exchange and queue naming
- MSG-07. Queue type
- MSG-09. Outbox and inbox
- MSG-10. Idempotency key strategy
- MSG-08. Retry and dead-letter policy
- MSG-11. Message serialization and envelope
- MSG-12. Correlation and tracing

## MSG-01. In-process events or a broker

- Question: Does this flow stay inside the service (a direct call or a domain event), or does it go through a broker?
- Ask when: the first event or the first background work item.
- Options: In process: a direct call, or a domain event handled in the same transaction. Broker: RabbitMQ between services, or for work that must survive a restart.
- Recommended: In process inside one deployable. A broker only between services, or when work must survive a restart. A broker adds a network hop, a second store, and new failure modes.
- Choose another option when: another service consumes the event. The work is slow and must not hold the request. A restart must not lose the work.
- Depends on: none.
- Revisit when: a second deployable needs the event.
- Check: judge.
- Detect: none.

## MSG-03. Broker choice

- Question: Which broker carries messages between services?
- Ask when: the first message between two services.
- Options: RabbitMQ 4.4: AMQP 0-9-1, quorum queues, streams, open source (MPL 2.0). Azure Service Bus: managed, sessions, no server to run. Kafka: a log for event streaming and replay, more operations work.
- Recommended: RabbitMQ 4.4 with quorum queues for a new broker. An existing broker on 4.3 keeps its version until its upgrade. It fits command and event messaging between services, runs in a container in tests, and Wolverine and EasyNetQ support it.
- Choose another option when: the platform is Azure-only and the team does not run servers: Azure Service Bus. The need is a replayable event log with long retention: Kafka.
- Depends on: MSG-01.
- Revisit when: the community support of the minor version ends (4.3: 2027-01-31, 4.4: 2027-07-31). Throughput needs a log.
- Check: judge.
- Detect: DET-MSG-03.

## MSG-04. Messaging library

- Question: Which library does the service use for messaging: Wolverine, raw RabbitMQ.Client, or EasyNetQ?
- Ask when: the first publisher or consumer in the service.
- Options: Wolverine 6 (MIT): handlers, durable outbox and inbox on EF Core, retries, RabbitMQ transport. Raw RabbitMQ.Client 7: full control, own outbox, own retry. EasyNetQ 8 (MIT): simple publish and subscribe, no outbox, no retry. MassTransit 9: commercial license.
- Recommended: Wolverine 6 for a service that writes to a database and publishes (the common case). Raw RabbitMQ.Client 7 for interop with a system in another language, or for infrastructure code. EasyNetQ 8 only where it exists.
- Choose another option when: the service only forwards messages and has no database: raw client or EasyNetQ. The service runs on .NET 8: Wolverine 5 (end of life October 2026) or the raw client.
- Depends on: MSG-01, MSG-03.
- Revisit when: a raw-client service gets its third message type or its own retry code.
- Check: judge.
- Detect: DET-MSG-03 (raw client or EasyNetQ). The Wolverine rule is in `dev-stack:wolverine`.

## MSG-05. Message contract location and versioning

- Question: Where do the message types live, and how do they change?
- Ask when: the first message that another service reads.
- Options: One `<Service>.Contracts` project per publishing service. A shared contracts project for all services. JSON schema files only.
- Recommended: One `<Service>.Contracts` project per publishing service. It has records only, no dependencies, pinned wire names, additive changes, and contract tests that compare the serialized JSON with stored samples. Consumers in other languages get the JSON samples.
- Choose another option when: the consumers are not .NET: JSON schema or samples as the main contract, generated types optional.
- Depends on: MSG-04.
- Revisit when: a breaking change is needed: add `v2` next to `v1`.
- Check: judge (a contract sample test).
- Detect: none.

## MSG-06. Exchange and queue naming

- Question: How are exchanges, routing keys, queues, and dead-letter queues named?
- Ask when: the first exchange or queue.
- Options: The catalog pattern below. A pattern that the platform team already uses.
- Recommended: Lowercase, dot-separated, kebab-case words. Exchange `{publisher}.events` (topic) and `{service}.commands`. Routing key `{entity}.{event}.v{n}`. Queue `{consumer}.{handler-or-purpose}`. Dead-letter queue `{service}.dlq`, one per service. A dead-letter queue per listener only when another team owns that queue.
- Choose another option when: the broker already has a naming scheme.
- Depends on: MSG-03.
- Revisit when: two services use the same queue name.
- Check: judge.
- Detect: none.

## MSG-07. Queue type

- Question: Which queue type do durable work queues use?
- Ask when: the first durable queue.
- Options: Quorum: replicated (Raft), data safety, poison message handling. Classic: one node, no replication. Stream: an append-only log with replay.
- Recommended: Quorum queues for all durable work queues, declared explicitly in code (`x-queue-type: quorum`), plus `default_queue_type = quorum` on each vhost as a safety net.
- Choose another option when: the queue is temporary and exclusive (a reply queue): classic, exclusive, auto-delete. Consumers need replay: a stream.
- Depends on: MSG-03.
- Revisit when: a queue needs replay or very high fan-out.
- Check: `rabbitmqctl list_queues name type`, judge.
- Detect: DET-MSG-07.

## MSG-09. Outbox and inbox

- Question: How does the service make a database write and a publish happen together?
- Ask when: the first publish that follows a database write.
- Options: Wolverine durable outbox and inbox. An own outbox table and a background publisher with confirms. Publish after the commit (loses messages).
- Recommended: The Wolverine durable outbox when the service uses Wolverine (`dev-stack:wolverine`). Without Wolverine: an own outbox table, written in the same `SaveChangesAsync`. A `BackgroundService` publishes with confirms and marks a row sent after the confirm (`references/outbox.md`).
- Choose another option when: a lost message is acceptable (a cache refresh hint): publish after the commit, and say so in a comment.
- Depends on: MSG-04.
- Revisit when: the outbox table grows: add a cleanup job for sent rows.
- Check: judge.
- Detect: none.

## MSG-10. Idempotency key strategy

- Question: How does a consumer handle a message that comes a second time?
- Ask when: the first consumer that writes data.
- Options: Natural idempotency: a unique index on the business key, an upsert with a version check, or a state check. A table of handled message IDs. No protection.
- Recommended: Make the effect naturally idempotent (unique index on the business key, or an upsert with a version check). A table of handled message IDs only for an effect that cannot be idempotent, written in the same transaction as the effect.
- Choose another option when: the effect is an external call (payment, mail). Then use the message ID table, and pass an idempotency key to the external API when it has one.
- Depends on: MSG-09.
- Revisit when: duplicates show in the data.
- Check: judge (a test that handles the same message two times).
- Detect: none.

## MSG-08. Retry and dead-letter policy

- Question: How does the consumer retry, and where do failed messages go?
- Ask when: the first consumer.
- Options: Wolverine error policies (`dev-stack:wolverine`). Raw client: `requeue: false` to a dead-letter exchange, DLX plus TTL for delayed retry, or the quorum delayed retry of RabbitMQ 4.3. EasyNetQ: no built-in retry.
- Recommended: Transient errors: 3 immediate retries with cooldown, then 2 delayed retries with jitter, then the dead-letter queue. Permanent errors: the dead-letter queue at once. On the broker: a dead-letter policy on each work queue, `delivery-limit` 20, and an alert when a dead-letter queue is not empty. Exception: a quorum queue that a Wolverine durable inbox reads gets `delivery-limit` -1 (Wolverine `DeliveryLimitForDurableQuorumQueues(-1)`, WolverineFx 6.51.3 or later). The inbox owns the retries, and each reconnect or restart returns the in-flight messages to the queue (`dev-stack:wolverine`).
- Choose another option when: a message is worthless after a short time: a TTL and discard on purpose.
- Depends on: MSG-04, MSG-06.
- Revisit when: the dead-letter queue fills in normal operation.
- Check: judge.
- Detect: none.

## MSG-11. Message serialization and envelope

- Question: How are messages serialized, and where does the type name go?
- Ask when: the first message.
- Options: System.Text.Json with camelCase and the type in the AMQP `type` property. Newtonsoft.Json. CloudEvents. Protobuf.
- Recommended: System.Text.Json, `application/json`, camelCase (`JsonSerializerDefaults.Web`), a stable type name in the AMQP `type` property (or the Wolverine `message-type` header), and the library's own envelope.
- Choose another option when: the consumers require CloudEvents or a schema registry.
- Depends on: MSG-05.
- Revisit when: a consumer cannot read the format.
- Check: judge (a contract sample test).
- Detect: none.

## MSG-12. Correlation and tracing

- Question: How do traces and log lines follow a message across services?
- Ask when: the first message between services.
- Options: OpenTelemetry with the built-in sources of the library. Own headers and logs only.
- Recommended: OpenTelemetry tracing with the built-in propagation of the library (`AddSource("RabbitMQ.Client.*")`, or `AddSource("Wolverine")`). Set `MessageId` and `CorrelationId` on each message and log both. Do not add the prerelease package `RabbitMQ.Client.OpenTelemetry` without approval.
- Choose another option when: the platform has no OpenTelemetry collector: logs with both IDs.
- Depends on: OPS decisions of `dev-stack:dotnet-observability`.
- Revisit when: `RabbitMQ.Client.OpenTelemetry` is stable.
- Check: judge.
- Detect: none.
