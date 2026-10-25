# Sources

Checked on 2026-10-25 (versions and the facts in "Checked on 2026-10-25"), 2026-10-19, and 2026-10-16 (broker behavior). The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- RabbitMQ documentation
- Client documentation
- Checked by compile and run
- Checked on 2026-10-25
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| RabbitMQ server | 4.4.0 (2026-10-20), support to 2027-07-31. 4.3.7 (image 2026-10-22), support to 2027-01-31 | [Release information](https://www.rabbitmq.com/release-information), [4.4.0 release notes](https://github.com/rabbitmq/rabbitmq-server/blob/main/release-notes/4.4.0.md), [4.3.7 release notes](https://github.com/rabbitmq/rabbitmq-server/blob/main/release-notes/4.3.7.md) |
| Docker image | `rabbitmq:4.4.0-management-alpine`, `rabbitmq:4.3.7-management`, `rabbitmq:4.3-management-alpine` | [Docker Hub rabbitmq](https://hub.docker.com/_/rabbitmq) |
| RabbitMQ.Client | 7.2.2 (2026-08-05), Apache-2.0 OR MPL-2.0, still the newest | [nuget.org RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client) |
| RabbitMQ.Client.OpenTelemetry | 1.0.0-rc.2 (prerelease) | [nuget.org](https://www.nuget.org/packages/RabbitMQ.Client.OpenTelemetry) |
| EasyNetQ | 8.1.11 (2026-10-14), MIT. 9.0.0-alpha.3 | [nuget.org EasyNetQ](https://www.nuget.org/packages/EasyNetQ) |
| Testcontainers.RabbitMq | 4.16.0, default image `rabbitmq:3.11`, still the newest |
| .NET | 10.0.13 (2026-10-13), SDK 10.0.402 | [releases.json](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json) | [nuget.org](https://www.nuget.org/packages/Testcontainers.RabbitMq) |
| MassTransit | 9.2.4, commercial license (context only) | [nuget.org MassTransit](https://www.nuget.org/packages/MassTransit) |

## RabbitMQ documentation

- [Quorum queues](https://www.rabbitmq.com/docs/quorum-queues): replication, `delivery-limit`, dead-letter strategy, global QoS.
- [Dead letter exchanges](https://www.rabbitmq.com/docs/dlx)
- [Policies](https://www.rabbitmq.com/docs/policies)
- [Consumer acknowledgements and publisher confirms](https://www.rabbitmq.com/docs/confirms)
- [Consumer prefetch](https://www.rabbitmq.com/docs/consumer-prefetch)
- [Consumers](https://www.rabbitmq.com/docs/consumers): consumer timeout, single active consumer.
- [Virtual hosts](https://www.rabbitmq.com/docs/vhosts): default queue type.
- [RabbitMQ 4.0 and 4.3 release notes](https://github.com/rabbitmq/rabbitmq-server/releases): removed features (mirrored queues, lazy mode, transient queues), deprecated global QoS.
- [Management HTTP API](https://www.rabbitmq.com/docs/http-api-reference): health checks, queue get.

## Client documentation

- [.NET client API guide](https://www.rabbitmq.com/client-libraries/dotnet-api-guide)
- [RabbitMQ.Client v7 migration guide](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/main/v7-MIGRATION.md): async API, `ea.Body` lifetime, confirms.
- [RabbitMQ tutorial 7, publisher confirms (.NET)](https://github.com/rabbitmq/rabbitmq-tutorials/tree/main/dotnet): the confirm pattern (paraphrased).
- [EasyNetQ wiki](https://github.com/EasyNetQ/EasyNetQ/wiki): connection string, subscriptions, error queue, scheduler.
- [Testcontainers for .NET, RabbitMQ module](https://dotnet.testcontainers.org/modules/rabbitmq/)

## Checked by compile and run

On 2026-10-19 with SDK 10.0.402 and the image `rabbitmq:4.3-management-alpine`:

- Compiled: each C# block in `SKILL.md`, `client.md`, `outbox.md`, `easynetq.md`, and `testing.md` (RabbitMQ.Client 7.2.2, EasyNetQ 8.1.11, Testcontainers.RabbitMq 4.16.0), with `-warnaserror` and `AnalysisLevel` latest-recommended.
- Run: the own outbox of `outbox.md` against a real broker. The broker was stopped, an order was accepted, the service restarted, the broker started, and the event reached the quorum queue with its AMQP properties.
- Run: `new RabbitMqBuilder()` gives CS0618. `ConsumerDispatchConcurrency` defaults to 1.

## Checked on 2026-10-25

| Fact | Version | Source |
|------|---------|--------|
| `basic.nack` increments only `acquired-count`. `basic.reject` and a lost connection increment `delivery-count`, which the `delivery-limit` counts. A consumer timeout does not count. | 4.3, 4.4 | [Quorum queues, poison message handling](https://www.rabbitmq.com/docs/quorum-queues) |
| Delayed retry: `x-delayed-retry-type` (`disabled`, `all`, `failed`, `returned`), `-min`, `-max` in ms, policy keys `delayed-retry-*`. Not run. | 4.3, 4.4 | [Quorum queues](https://www.rabbitmq.com/docs/quorum-queues) |
| Quorum queues hold a message with `x-opt-delivery-time` until that time. Not run. | 4.4.0 | 4.4.0 release notes |
| A change of `delivery-limit` in a policy now applies to existing quorum queues. Before 4.4 it did not. | 4.4.0 | 4.4.0 release notes |
| On a consumer timeout, the broker sends `basic.cancel` to a client with `consumer_cancel_notify`. `consumer_timeout_response` setting. | 4.3, 4.3.7 | [Quorum queues](https://www.rabbitmq.com/docs/quorum-queues), 4.3.7 release notes |
| The docs say that global QoS on a quorum queue gives a channel error. The run of 2026-10-24 on 4.3.6 got no error. The skill keeps "do not depend on an error". | 4.3.6 | [Quorum queues](https://www.rabbitmq.com/docs/quorum-queues) |
| The client advertises `consumer_cancel_notify` (string in `RabbitMQ.Client.dll`), and does not consume again after `basic.cancel`: it calls `HandleBasicCancelAsync`, then `UnregisteredAsync`. | 7.2.2 | DeepWiki rabbitmq/rabbitmq-dotnet-client, the nuget package |
| The dispatcher awaits `HandleBasicDeliverAsync` in one loop for each channel. With concurrency 1, a `Task.Delay` in the handler stops other deliveries. | 7.2.2 | [AsyncConsumerDispatcher.cs](https://github.com/rabbitmq/rabbitmq-dotnet-client/blob/v7.2.2/projects/RabbitMQ.Client/ConsumerDispatching/AsyncConsumerDispatcher.cs) |
| Without `basic.qos`, the prefetch is 0 (no limit). | AMQP 0-9-1 | [Consumer prefetch](https://www.rabbitmq.com/docs/consumer-prefetch), DeepWiki |
| PostgreSQL treats NULL values as distinct in a unique index, unless `NULLS NOT DISTINCT` (PostgreSQL 15 and later). | PostgreSQL 15 to 18 | [Unique indexes](https://www.postgresql.org/docs/17/indexes-unique.html) |
| SQL Server allows one NULL in a unique index. EF Core adds the filter `IS NOT NULL` for a nullable column of a unique index. | SQL Server 2025, EF Core 10 | [Index design guide](https://learn.microsoft.com/sql/relational-databases/sql-server-index-design-guide), [EF Core indexes](https://learn.microsoft.com/ef/core/modeling/indexes#index-filter) |

## Credits

- RabbitMQ documentation and the rabbitmq-tutorials repository (Apache-2.0): the confirm pattern is paraphrased. A line for `plugins/dev-stack/CREDITS.md` is needed.
- No text was copied from community skills. Ideas only: the "outbox first" framing (codewithmukesh/dotnet-claude-kit, license not stated).
