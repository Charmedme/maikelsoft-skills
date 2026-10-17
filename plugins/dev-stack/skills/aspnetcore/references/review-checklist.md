# Review checklist: aspnetcore

Each check has a type. `tool` means an analyzer, the compiler, or a `rules.json` rule finds it. Run the tools first (`SKILL.md`, section "Check"). `judge` means a person or an agent reads the code. Language checks (async, nullability, `TimeProvider`, logging templates) are in `dev-stack:csharp`. Security checks are in `dev-stack:secure-coding`.

## Contents

- Host and DI
- Configuration and options
- Pipeline
- Endpoints
- Errors
- Validation
- Outbound HTTP
- Health, background work, shutdown
- Packages and decisions
- Tests

## Host and DI

| # | Check | Type |
|---|-------|------|
| 1 | The host uses `WebApplication.CreateBuilder` or `Host.CreateApplicationBuilder`. No `WebHost`, no new `Startup` class. | tool (ASPDEPR008, ASP-WEBHOST, ASP-STARTUP-CLASS) |
| 2 | No `BuildServiceProvider()` in application code. | tool (ASP0000, ASP-BUILD-PROVIDER) |
| 3 | `ValidateScopes` and `ValidateOnBuild` are on in each environment, or a startup test covers the DI graph. | judge |
| 4 | No scoped service in a singleton or hosted service. Hosted services use `CreateAsyncScope()` per unit of work. | judge (`ValidateScopes` at run time) |
| 5 | Registrations are in `AddX` methods per feature. `Program.cs` stays short. | judge |
| 6 | An interface exists only at a real seam, not for each class. | judge |

## Configuration and options

| # | Check | Type |
|---|-------|------|
| 7 | Each settings section binds to an options class with validation and `ValidateOnStart`. | tool (ASP-OPTIONS-NO-VALIDATE, ASP-OPTIONS-CONFIGURE-SECTION) |
| 8 | Nested options use `[ValidateObjectMembers]`, lists use `[ValidateEnumeratedItems]`. | judge |
| 9 | Services read `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`, not `IConfiguration["..."]`. | judge |
| 10 | A singleton that must see changes uses `IOptionsMonitor<T>`. No `IOptionsSnapshot<T>` in a singleton. | judge |
| 11 | No `null` in configuration where the author wants the class default (.NET 10 binds `null`). | judge |
| 12 | No secrets in `appsettings*.json`. | judge (secret scan in `dev-stack:secure-coding`) |

## Pipeline

| # | Check | Type |
|---|-------|------|
| 13 | Order: forwarded headers, exception handler, status code pages, HSTS, HTTPS, CORS, authentication, authorization, rate limiter, endpoints. | tool (ASP-ORDER-CORS, ASP-ORDER-AUTH, ASP-ORDER-FORWARDED) and judge |
| 14 | Forwarded headers use `KnownIPNetworks` or `KnownProxies`. No `KnownNetworks`. | tool (ASPDEPR005) |
| 15 | CORS uses one named policy with exact origins from configuration. | judge |
| 16 | A fallback authorization policy exists, or each group has `RequireAuthorization`. Public endpoints have `AllowAnonymous()`. | judge |
| 17 | The rate limiter sets `RejectionStatusCode = 429`, and sensitive endpoints have a policy. | judge |
| 18 | No `UseDeveloperExceptionPage()`. | tool (ASP-DEV-EXCEPTION-PAGE) |

## Endpoints

| # | Check | Type |
|---|-------|------|
| 19 | New endpoints follow the endpoint style of the project. No mix of minimal APIs and controllers. | judge (ASP-DET-API-01 lists controllers) |
| 20 | Endpoints are in a `MapX` per feature with `MapGroup` and named handler methods. | judge |
| 21 | Handlers return `TypedResults` with a declared `Results<...>` type. | tool (ASP-RESULTS-UNTYPED) |
| 22 | Each handler takes a `CancellationToken` and passes it to each I/O call. | tool (CA2016) and judge |
| 23 | Requests and responses are `sealed record` types. No EF entity in a body. `DateTimeOffset` for instants. | judge |
| 24 | Status codes match the table in `references/endpoints-errors.md`. A create returns 201 with `Location`. | judge |
| 25 | Endpoints call a handler class directly. No mediator, no AutoMapper. | tool (ASP-PKG-MEDIATR, ASP-PKG-AUTOMAPPER) |
| 26 | JSON settings use `ConfigureHttpJsonOptions` (minimal APIs) or `AddJsonOptions` (controllers). An existing API keeps its settings. | judge |
| 27 | No `.WithOpenApi()`. The endpoint declares each status code. No Swashbuckle in a new project. | tool (ASPDEPR002, ASP-WITH-OPENAPI, ASP-DET-API-09) |

## Errors

| # | Check | Type |
|---|-------|------|
| 28 | `AddProblemDetails`, `UseExceptionHandler`, and `UseStatusCodePages` are in `Program.cs`. | judge (ASP-DET-API-04) |
| 29 | No try/catch in endpoints that turns each exception into a 500. | judge |
| 30 | Expected failures are results with a stable `code`, not exceptions. | judge |
| 31 | No exception message or stack trace in a response. | tool (ASP-EXCEPTION-MESSAGE) and judge |
| 32 | An `IExceptionHandler` that returns `true` logs the exception, or `SuppressDiagnosticsCallback` keeps the diagnostics. | judge |

## Validation

| # | Check | Type |
|---|-------|------|
| 33 | Minimal APIs on .NET 10 call `AddValidation()`, or the project uses FluentValidation explicitly. | judge |
| 34 | No `AddFluentValidation`, `AddFluentValidationAutoValidation`, or `FluentValidation.AspNetCore`. | tool (ASP-FV-AUTOVALIDATION, ASP-PKG-FV-ASPNETCORE) |
| 35 | FluentValidation calls use `ValidateAsync` with the request token. | judge |
| 36 | One contract has one validation library. | judge |
| 37 | Rules about state and uniqueness are not only in request validation. | judge |

## Outbound HTTP

| # | Check | Type |
|---|-------|------|
| 38 | No `new HttpClient(` outside tests, except one static client with `SocketsHttpHandler`. | tool (ASP-NEW-HTTPCLIENT) |
| 39 | No typed client in a singleton or hosted service. | judge |
| 40 | Each client has one resilience handler. | judge |
| 41 | Calls that are not idempotent do not retry. | judge |
| 42 | `HttpClient.Timeout` is above the total timeout of the pipeline. | judge |
| 43 | No `Microsoft.Extensions.Http.Polly`, no `AddTransientHttpErrorPolicy`, no `AddPolicyHandler`. | tool (ASP-PKG-HTTP-POLLY, ASP-POLLY-POLICY) |
| 44 | The caller handles `TimeoutRejectedException` and `BrokenCircuitException` where the call result matters. | judge |

## Health, background work, shutdown

| # | Check | Type |
|---|-------|------|
| 45 | Liveness runs no checks. Readiness runs the `ready` checks. Both are anonymous and short-circuited. | judge |
| 46 | Health endpoints are not on the public ingress, or they use `RequireHost` on an internal port. | judge |
| 47 | A `BackgroundService` uses `PeriodicTimer` with `TimeProvider`, a scope per iteration, and a catch per iteration that lets `OperationCanceledException` pass. | judge |
| 48 | Work that must finish before traffic is in `StartAsync`, not before the first `await` in `ExecuteAsync`. | judge |
| 49 | Each call in a loop gets `stoppingToken`. `ShutdownTimeout` is below the orchestrator grace period. | judge |
| 50 | No `Task.Run` fire-and-forget from a request. | judge |

## Packages and decisions

| # | Check | Type |
|---|-------|------|
| 51 | Each new package has the approval of the human (name, version, license, reason). | judge |
| 52 | Each new decision of `references/decisions.md` has an accepted answer in `docs/dev-team/decisions.md`, or the output marks it "assumed". | tool (`decisioncheck.py` in `dev-team:team-decisions`, with the ASP-DET rules) |
| 53 | `dotnet list package --deprecated --vulnerable --include-transitive` shows no rows. | tool |

## Tests

| # | Check | Type |
|---|-------|------|
| 54 | One HTTP test per new endpoint: status code, `Location`, ProblemDetails shape, 401 and 403. | judge (`dev-stack:dotnet-testing`) |
| 55 | One test starts the host with `ValidateOnBuild` and `ValidateScopes`. | judge |
| 56 | One test shows that bad configuration stops the start. | judge |
