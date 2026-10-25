# Sources

Checked on 2026-10-19 and 2026-10-25. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Wolverine documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| WolverineFx | 6.53.0 (2026-10-25), MIT, `net9.0` and `net10.0` | [nuget.org WolverineFx](https://www.nuget.org/packages/WolverineFx) |
| WolverineFx.RabbitMQ, .EntityFrameworkCore, .Postgresql | 6.53.0 | [nuget.org](https://www.nuget.org/packages?q=WolverineFx) |
| WolverineFx.RuntimeCompilation | 6.53.0 | [nuget.org](https://www.nuget.org/packages/WolverineFx.RuntimeCompilation) |
| JasperFx | 2.95.1 (2026-10-25) | [nuget.org JasperFx](https://www.nuget.org/packages/JasperFx) |
| Wolverine 5 | Fixes for severe bugs only, end of life October 2026 | [Support policy](https://wolverinefx.net/introduction/support-policy.html) |
| CritterWatch | Commercial monitoring by JasperFx (context only) | [JasperFx products](https://jasperfx.net/our-products/) |

## Wolverine documentation

- [Migration guide, key changes in 6.0](https://wolverinefx.net/guide/migration.html): service location, RuntimeCompilation, forwarders, .NET 8.
- [Message handlers](https://wolverinefx.net/guide/handlers/): discovery, parameters, return values.
- [Cascading messages](https://wolverinefx.net/guide/handlers/cascading.html)
- [Durable outbox and inbox](https://wolverinefx.net/guide/durability/)
- [EF Core integration](https://wolverinefx.net/guide/durability/efcore/): `AddDbContextWithWolverineIntegration`, `IDbContextOutbox<T>`, transactions.
- [Error handling](https://wolverinefx.net/guide/handlers/error-handling.html)
- [RabbitMQ transport](https://wolverinefx.net/guide/messaging/transports/rabbitmq/): quorum queues, channel creation, dead letters, listener modes.
- [Message identity and versioning](https://wolverinefx.net/guide/messages.html)
- [Code generation](https://wolverinefx.net/guide/codegen.html): `TypeLoadMode`, `codegen write`, `CritterStackDefaults`.
- [Test support, tracked sessions](https://wolverinefx.net/guide/testing.html)
- [llms-full.txt](https://wolverinefx.net/llms-full.txt): the full documentation in one file.

## Checked by compile and run

On 2026-10-19 with SDK 10.0.402, WolverineFx 6.51.0, PostgreSQL 18.6, and `rabbitmq:4.3-management-alpine`:

- Compiled: each C# block in `SKILL.md` and the references, with `-warnaserror` and `AnalysisLevel` latest-recommended.
- Run: the `Program.cs` of `configuration.md` with `WebApplicationFactory`. A tracked session handled a cancel command. A business error ran one time and went to the error queue. The cancel queue was a quorum queue. A message saved while the broker was down reached the exchange after a service restart.
- Run: a plain `AddDbContext` failed the first message. A policy for all exceptions retried the business error. Without the durable outbox, the restart lost the message.
- Run: an app that ends with `RunJasperFxCommands(args)` failed under `WebApplicationFactory` with "The server has not been started" until the test set `JasperFxEnvironment.AutoStartHost = true`.
- Run: the default exchange type of `ToRabbitExchange` is fanout.
- From the research of the same day (compiled and run): a handler that returns a primitive is excluded, `InvokeAsync<int>` throws `IndeterminateRoutesException`, Dynamic mode without RuntimeCompilation does not start, Static mode without generated files throws `MissingPreBuiltTypesException`, the RabbitMQ listener default is `Inline` with prefetch 100.

## Checked on 2026-10-25

With SDK 10.0, WolverineFx 6.53.0, no database and no broker:

| Fact | How | Source |
|------|-----|--------|
| A handler that injects a typed `HttpClient` throws `InvalidServiceLocationException`. This is true for `AddHttpClient<IFoo, Foo>()` with and without a configure lambda. The host starts. The error comes on the first message. | Run | [Migration guide](https://wolverinefx.net/guide/migration.html), [DeepWiki JasperFx/wolverine](https://deepwiki.com/JasperFx/wolverine) |
| `opts.CodeGeneration.AlwaysUseServiceLocationFor<IFoo>()` fixes it. A handler that injects `IHttpClientFactory` works without it. | Run | The same |
| An `IForwardsTo<V2>` type without `RegisterMessageForwarder<V1, V2>()` has no handler: `InvokeAsync(v1)` throws `IndeterminateRoutesException`. With the registration, the V2 handler runs. | Run | [Message identity and versioning](https://wolverinefx.net/guide/messages.html) |
| `[MessageIdentity]` is in the namespace `Wolverine.Attributes`. | Compile | The same |
| `.ExternallyOwned()` exists on `ToRabbitQueue` and `ListenToRabbitQueue`. With it, `AutoProvision()` does not declare, purge, or delete the queue (GH-3064). | Compile, source | [RabbitMqQueue.cs](https://github.com/JasperFx/wolverine/blob/main/src/Transports/RabbitMQ/Wolverine.RabbitMQ/Internal/RabbitMqQueue.cs) |
| A declaration with other arguments than the existing queue fails with `PRECONDITION_FAILED`, and Wolverine retries the broker start. | Run with RabbitMQ 4.3 in the hard case `wolverine-hard` (2026-10-21) | `evals/dev-stack/cases/wolverine-hard.json` |
| Releases 6.52.0 and 6.53.0 do not change the facts of this skill (service location, forwarders, code generation, RabbitMQ defaults). | The runs above | [nuget.org WolverineFx](https://www.nuget.org/packages/WolverineFx) |

DeepWiki says that the service location error comes at host start. The run shows it on the first message in `Dynamic` mode. The skill follows the run.

## Credits

- Wolverine documentation (JasperFx/wolverine, MIT): paraphrased, linked above. A line for `plugins/dev-stack/CREDITS.md` is needed.
- No text from the paid JasperFx AI skills. Ideas only from codewithmukesh/dotnet-claude-kit (license not stated).
