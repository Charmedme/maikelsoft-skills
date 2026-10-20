# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Wolverine documentation
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| WolverineFx | 6.51.0 (2026-10-15), MIT, `net9.0` and `net10.0` | [nuget.org WolverineFx](https://www.nuget.org/packages/WolverineFx) |
| WolverineFx.RabbitMQ, .EntityFrameworkCore, .Postgresql, .SqlServer | 6.51.0 | [nuget.org](https://www.nuget.org/packages?q=WolverineFx) |
| WolverineFx.RuntimeCompilation | 6.51.0 | [nuget.org](https://www.nuget.org/packages/WolverineFx.RuntimeCompilation) |
| JasperFx | 2.90.2 (2026-10-16) | [nuget.org JasperFx](https://www.nuget.org/packages/JasperFx) |
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

## Credits

- Wolverine documentation (JasperFx/wolverine, MIT): paraphrased, linked above. A line for `plugins/dev-stack/CREDITS.md` is needed.
- No text from the paid JasperFx AI skills. Ideas only from codewithmukesh/dotnet-claude-kit (license not stated).
