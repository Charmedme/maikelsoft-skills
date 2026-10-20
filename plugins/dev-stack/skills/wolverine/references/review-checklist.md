# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Host and registrations
- Handlers
- Outbox and inbox
- Transport
- Failures
- Contracts
- Tests
- Decisions

## Host and registrations

| # | Check | Type |
|---|-------|------|
| 1 | The target framework is .NET 9 or 10 for Wolverine 6. | judge |
| 2 | Each `DbContext` that a handler injects uses `AddDbContextWithWolverineIntegration` or `optionsLifetime: ServiceLifetime.Singleton`. | tool (WOLV-DBCONTEXT-OPTIONS) |
| 3 | No lambda registration for a service that a handler injects. `AlwaysUseServiceLocationFor<T>()` only with a reason. | judge |
| 4 | No `RestoreV5Defaults()`. | tool (WOLV-RESTORE-V5) |
| 5 | `WolverineFx.RuntimeCompilation` is referenced when any environment uses `Dynamic` or `Auto`. | judge |
| 6 | Production uses `TypeLoadMode.Static`, `Internal/Generated/` is in git, and CI runs `codegen write` with a diff check. | tool (WOLV-DYNAMIC-PROD), judge |
| 7 | `opts.ApplicationAssembly` is set when tests use `WebApplicationFactory`. | judge |

## Handlers

| # | Check | Type |
|---|-------|------|
| 8 | Handlers follow the naming convention and are static where they can be. | judge |
| 9 | No handler returns a primitive. `InvokeAsync<T>` uses a record `T`. | judge |
| 10 | Messages cascade from the return value. No `IMessageBus` in domain services, no own wrapper over Wolverine. | judge |
| 11 | No MassTransit, NServiceBus, or MediatR interfaces. | tool (WOLV-FOREIGN-IDIOM) |
| 12 | Handlers with `AutoApplyTransactions` do not call `SaveChangesAsync` themselves. | judge |
| 13 | Commands use `SendAsync` or `InvokeAsync`. Events use `PublishAsync`. | judge |

## Outbox and inbox

| # | Check | Type |
|---|-------|------|
| 14 | The message store, the EF Core transactions, `AutoApplyTransactions`, the durable outbox on all senders, and the durable inbox on business listeners are all configured. | judge |
| 15 | Endpoints and background services use `IDbContextOutbox<T>` or `InvokeAsync`. No `SaveChangesAsync` followed by `PublishAsync`. | judge |
| 16 | Business duplicates are handled by a natural key or `DeduplicationId`, not by the inbox window alone. | judge |
| 17 | `InboxStaleTime` is longer than the longest handler with its retries. | judge |

## Transport

| # | Check | Type |
|---|-------|------|
| 18 | `UseQuorumQueues()` and both publisher confirm flags on the RabbitMQ transport. | judge |
| 19 | The topology is declared (`AutoProvision()` or by operations). The exchange type fits the consumers (fanout is the default). | judge |
| 20 | One dead-letter queue per service with `CustomizeDeadLetterQueueing`. | judge |
| 21 | The listener mode fits each endpoint. No `BufferedInMemory` for business messages. | judge |

## Failures

| # | Check | Type |
|---|-------|------|
| 22 | An error policy for each exception type. No single policy for `Exception`. | judge |
| 23 | Business and validation errors go to the error queue at once, or `Discard()` on purpose. | judge |
| 24 | Transient errors: cooldown retries, then scheduled retries with jitter, then the error queue. | judge |
| 25 | No catch-and-log in a handler. | judge |

## Contracts

| # | Check | Type |
|---|-------|------|
| 26 | Each contract has `[MessageIdentity]` with a version. | judge |
| 27 | Each `IForwardsTo<T>` type is registered with `RegisterMessageForwarder`. | judge |
| 28 | System.Text.Json (the default). No `UseNewtonsoftForSerialization` in new code. | tool (WOLV-NEWTONSOFT) |

## Tests

| # | Check | Type |
|---|-------|------|
| 29 | Tests use tracked sessions, not `Task.Delay`. | tool (WOLV-TEST-DELAY), judge |
| 30 | Tests call `DisableAllExternalWolverineTransports()` and `RunWolverineInSoloMode()`, and set `JasperFxEnvironment.AutoStartHost` when the app uses `RunJasperFxCommands`. | judge |
| 31 | A test covers the business error path (error queue or no retry). | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 32 | New code follows the accepted MSG decisions, or the report marks them as assumed (MSG-08, MSG-09, MSG-13). | judge |
