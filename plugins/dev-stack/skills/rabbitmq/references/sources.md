# Sources

Checked on 2026-10-19 (versions) and 2026-10-16 (broker behavior). The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- RabbitMQ documentation
- Client documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| RabbitMQ server | 4.3.6 (2026-09-16), community support to 2027-01-31 | [Release information](https://www.rabbitmq.com/release-information) |
| Docker image | `rabbitmq:4.3.6-management`, `rabbitmq:4.3-management-alpine` | [Docker Hub rabbitmq](https://hub.docker.com/_/rabbitmq) |
| RabbitMQ.Client | 7.2.2 (2026-08-05), Apache-2.0 OR MPL-2.0 | [nuget.org RabbitMQ.Client](https://www.nuget.org/packages/RabbitMQ.Client) |
| RabbitMQ.Client.OpenTelemetry | 1.0.0-rc.2 (prerelease) | [nuget.org](https://www.nuget.org/packages/RabbitMQ.Client.OpenTelemetry) |
| EasyNetQ | 8.1.11 (2026-10-14), MIT. 9.0.0-alpha.3 | [nuget.org EasyNetQ](https://www.nuget.org/packages/EasyNetQ) |
| Testcontainers.RabbitMq | 4.16.0, default image `rabbitmq:3.11` | [nuget.org](https://www.nuget.org/packages/Testcontainers.RabbitMq) |
| MassTransit | 9.2.3, commercial license (context only) | [nuget.org MassTransit](https://www.nuget.org/packages/MassTransit) |

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

## Credits

- RabbitMQ documentation and the rabbitmq-tutorials repository (Apache-2.0): the confirm pattern is paraphrased. A line for `plugins/dev-stack/CREDITS.md` is needed.
- No text was copied from community skills. Ideas only: the "outbox first" framing (codewithmukesh/dotnet-claude-kit, license not stated).
