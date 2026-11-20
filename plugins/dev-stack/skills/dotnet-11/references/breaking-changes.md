# Breaking changes for app code

The changes of .NET 11, ASP.NET Core 11, and EF Core 11 that can change app code at the move from `net10.0`. Each row says what to do. Status: GA on 2026-11-10. Microsoft keeps the full lists on Microsoft Learn.

## Contents

- Runtime and libraries
- Packages in the shared framework
- ASP.NET Core 11
- EF Core 11
- Compiler and SDK

## Runtime and libraries

| Change | Effect | What to do |
|--------|--------|------------|
| CPU baseline `x86-64-v2` for x86 and x64 | The runtime does not start on an older CPU. | Check each host, VM, and build agent (`SKILL.md`, rule 6). |
| `IHost.RunAsync` and `StopAsync` throw when a `BackgroundService` fails | One failed service rethrows its exception. More than one gives an `AggregateException`. The exit code is not 0. | Keep the catch per iteration. Change tests that expect a normal return. |
| DI scope disposal collects the exceptions | A scope disposal with failures in more than one service throws an `AggregateException`. | Change a catch that expects one exception type. |
| Empty configuration strings | An empty string in a later provider replaces the value of an earlier provider. | Remove empty entries that mean "not set". Validate options with `ValidateOnStart`. |
| `DateOnly.TryParse` and `TimeOnly.TryParse` | They throw `ArgumentException` for an invalid `DateTimeStyles` value or format. Before, they returned `false`. | Change a test that expects `false`. Pass a valid style. |
| `Math.Round` and `MathF.Round` with digits | The runtime rounds correctly. The last digit can change. | Check snapshot tests of amounts. |
| `decimal` and `BigInteger` conversions from floating point | Correctly rounded | Same as the previous row. |
| Floating-point conversions to small integer types | The value saturates (for example `byte` stops at 255). | Check code that depends on overflow. |
| `ChangeToken.OnChange` | An async lambda binds to the new `Task` overload, not `async void`. The timing changes. | Check code that expects the callback to return at once. |
| HttpClient metrics with many values | Observable counters | Check dashboards and alert rules. |
| `NamedPipeClientStream` constructor with `isConnected` | SYSLIB0063 | Use the constructor without `isConnected`. |
| ZIP reads | The reader checks CRC32. A damaged entry throws. | Handle the exception where the app reads files from users. |
| `DOTNET_RuntimeAsync` | Removed | Use `<Features>runtime-async=on</Features>` (decision NET-02). |

## Packages in the shared framework

These 9 libraries are now in the shared framework:

- `Microsoft.Extensions.Caching.Abstractions`
- `Microsoft.Extensions.Configuration.Abstractions`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Diagnostics.Abstractions`
- `Microsoft.Extensions.FileProviders.Abstractions`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Options`
- `Microsoft.Extensions.Primitives`

A project that targets only `net11.0` and references one of them gets NU1510. Remove the `PackageReference`. Remove the `PackageVersion` in `Directory.Packages.props` too, when no other project needs it. A project with `net10.0;net11.0` gets no warning, so keep the reference there.

## ASP.NET Core 11

| Change | Effect | What to do |
|--------|--------|------------|
| `ConcurrencyLimiter` middleware removed | `UseConcurrencyLimiter` and `services.AddConcurrencyLimiter` give CS1061. | Use `AddRateLimiter` with `RateLimitPartition.GetConcurrencyLimiter`, and `UseRateLimiter` (`SKILL.md`, rule 9). |
| `Microsoft.OpenApi` 3.x, default `OpenApi3_2` | Transformers and snapshot tests of the document change. `Asp.Versioning.OpenApi` 10.2.3 allows only `Microsoft.OpenApi` below 3.0. | Follow `dev-stack:aspnetcore-openapi`. A versioned API stays on .NET 10. |
| MVC compatibility options removed | Code that sets `CompatibilityVersion` does not compile. | Remove the call. |
| OpenTelemetry HTTP tags by default | The hosting activity has the HTTP server tags. 5xx responses get `error.type`. | Update `OpenTelemetry.Instrumentation.AspNetCore` to prevent duplicate tags. Follow `dev-stack:dotnet-observability`. |
| Kestrel is stricter | HTTP/2 and HTTP/3 requests with connection-specific headers fail. | Check proxies that translate HTTP/1.1 to HTTP/2. |
| SqlClient Active Directory authentication | The Entra ID code moved to `Microsoft.Data.SqlClient.Extensions.Azure`. | Add that package after the approval of the human (stack rule 5). |
| Blazor template without `app.UseAntiforgery()` | A new Blazor app uses the cross-origin protection. Forms get no antiforgery token. | Keep `UseAntiforgery()` in an existing app that needs tokens. Follow the `blazor` skill. |
| `[ValidatableType]` and `[SkipValidation]` | No longer experimental | Remove each ASP0029 suppression. |

## EF Core 11

EF Core 11 needs `net11.0`. A project on `net10.0` stays on EF Core 10. The rules for queries and mapping are in `dev-stack:efcore` (`references/ef11.md`).

| Change | Effect | What to do |
|--------|--------|------------|
| Tools package without Design | `dotnet ef` cannot find the design package. | Add `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets="all"`. |
| `EFOptimizeContext` removed | The build ignores the property. | Use `EFScaffoldModelStage` and `EFPrecompileQueriesStage`. |
| `UseSqlServer` default compatibility level 160 | EF writes SQL Server 2022 SQL. | Set `UseCompatibilityLevel` to the real level of the server. |
| `Migrate` throws without migrations | The app stops at startup. | Remove the call. |
| Split queries throw `DbQueryConcurrencyException` | Before, they returned empty collections when the data changed. | Retry, use a snapshot transaction, or use a single query. |
| `SqlVector<T>` not in `SELECT` by default | Vector properties are `null`. | Project them when you need them. |
| Microsoft.Data.SqlClient 7 | `ActiveDirectoryPassword` is obsolete. | Use `ActiveDirectoryDefault` or a managed identity. |
| EFCore.PG 11 | 11.0.0-rc.3 is the latest on 2026-11-19. No GA. | Keep EF Core 10 with EFCore.PG 10, or wait (`SKILL.md`, table in "First"). |

## Compiler and SDK

| Change | Effect | What to do |
|--------|--------|------------|
| `union`, `closed`, and `with(...)` in a collection expression | An identifier or method with that name does not compile. | Use `@union`, `@closed`, `@with(...)`. |
| `(X.Y) when` and `nameof(this.P)` in attributes | The parser reads some forms in a different way. | Fix the errors that the build shows. |
| `DOTNET_TEST_RUNNER` | The variable overrides the test runner of `global.json`. | Keep the runner in `global.json`. Do not set the variable in CI. |
| C# 15 missing case in a `switch` | Warning CS8509, not an error | Make CS8509 an error (`SKILL.md`, rule 1). |
