# ASP.NET Core 11 and .NET 11

Read this file only when the project targets `net11.0`. For `net10.0`, the rules in `SKILL.md` apply without change.

Status on 2026-10-27: .NET 11 is at RC 3 (11.0.0-rc.3, 2026-10-25), with "go-live" support. Microsoft expects GA in November 2026. .NET 11 is STS (24 months of support). .NET 10 is LTS. It stays the default for new projects (decision SOL-01 in `dev-stack:csharp`). The facts in this file come from the RC 2 documentation, and the list of breaking changes of 2026-10-27 adds no item for this file. Check the facts again after GA (stack rule 2). This file has no code examples, because the .NET 11 SDK is not installed in the authoring environment.

## Contents

- Host and background services
- Options
- Validation
- Endpoints and middleware
- Kestrel
- OpenAPI
- Telemetry
- Packages in the shared framework
- Upgrade steps

## Host and background services

- When a `BackgroundService` fails and `BackgroundServiceExceptionBehavior` is `StopHost` (the default), `RunAsync`, `StopAsync`, and `WaitForShutdownAsync` now throw. One failed service rethrows its exception. More than one gives an `AggregateException`. The process exits with a non-zero code.
  - Do nothing in most apps: the orchestrator then sees the failure and restarts the pod.
  - The rule to catch per iteration (rule 35) stays: a failed iteration must not stop the host.
- The .NET 10 change stays: `ExecuteAsync` runs fully in the background (rule 34).

## Options

- New: async options validation (`ValidateAsync` on `OptionsBuilder<T>`, `IAsyncValidateOptions<T>`, `IAsyncStartupValidator`). Use it for a check that needs I/O at the start, for example "the configured queue exists".
- From RC 1, `IAsyncValidateOptions<T>` derives from `IValidateOptions<T>`. A synchronous read (`IOptions<T>.Value`, `IOptionsMonitor<T>.CurrentValue`, `IOptionsSnapshot<T>.Value`) of an options type with only async validators throws `OptionsValidationException`.
  - Call `ValidateOnStart()` for each options type with an async validator, and read the options only after the host started.
  - `IOptionsSnapshot<T>` in a later scope and `IOptionsMonitor<T>` after a change create the options synchronously. When those paths matter, keep at least one synchronous validator (for example DataAnnotations).
  - A class that implements `IAsyncValidateOptions<T>` must also implement `Validate(string? name, T options)`. Return `ValidateOptionsResult.Skip` when it does not apply.
- The `[OptionsValidator]` generator reports SYSLIB1205 for an explicit `Validate` implementation.

## Validation

- `ValidatableTypeAttribute` and `SkipValidationAttribute` are no longer experimental. Remove the ASP0029 suppressions.
- Async validation for minimal APIs: `AsyncValidationAttribute` and `IAsyncValidatableObject` run in `AddValidation`. Keep I/O out of request validation by default. A uniqueness check still needs a unique index (`references/endpoints-errors.md`, section "Validation with AddValidation").
- `AddValidation` localizes messages without an extra package: call `AddLocalization()` before `AddValidation()`. The `ErrorMessage` value is the resource key. This matters for decision CFG-10 only when the server must return localized text.

## Endpoints and middleware

- `[ShortCircuit]` attribute: the attribute form of `.ShortCircuit()`. It works on minimal API handlers and on controllers. `.ShortCircuit()` still works.
- ASP.NET Core 11 removes the `ConcurrencyLimiter` middleware (obsolete since ASP.NET Core 8 with warning CS0618). There is no 11.x package. Use `AddRateLimiter` with `AddConcurrencyLimiter("policy", ...)` and `RequireRateLimiting("policy")`.
- The rate limiter returns accurate `Retry-After` headers.
- ASP.NET Core 11 removes the MVC compatibility options. Code that sets `CompatibilityVersion` or old MVC compatibility switches fails to compile.
- Response compression always adds `Vary: Accept-Encoding` when it is on.

## Kestrel

- HTTP/2 and HTTP/3: a request with `Connection`, `Proxy-Connection`, `Keep-Alive`, `TE` (other than `trailers`), `Transfer-Encoding`, or `Upgrade` is malformed and fails.
- HTTP/1.1: a request with both `Content-Length` and `Transfer-Encoding` still runs, but Kestrel closes the connection after the response (request smuggling defense). Earlier versions get the same change in servicing updates.
- What to do: check gateways and proxies that translate HTTP/1.1 to HTTP/2. Compliant clients see no change.

## OpenAPI

Detail is in `dev-stack:aspnetcore-openapi`. The short list:

- `Microsoft.AspNetCore.OpenApi` 11 moves to Microsoft.OpenApi 3.x. Document, operation, and schema transformers that change the models need changes.
- The default document version is OpenAPI 3.2. Set `options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1` when a tool does not read 3.2 yet.
- The document keeps each `[ProducesResponseType]` and each `.Produces()` per status code. Snapshot tests of the document change.
- Asp.Versioning.OpenApi 10.2.3 pins Microsoft.OpenApi below 3.0 and does not run on ASP.NET Core 11 yet. Keep versioned APIs on .NET 10 until a compatible release ships.

## Telemetry

- The hosting `Activity` now has the OpenTelemetry HTTP server tags by default (`http.request.method`, `http.route`, `http.response.status_code`, and others). `OpenTelemetry.Instrumentation.AspNetCore` is no longer needed for these tags. Update it to the latest version to prevent duplicate tags.
- `http.server.request.duration` gets an `error.type` dimension for 5xx responses. Check the cardinality budget of the metrics backend.
- `Activity.StatusDescription` is no longer set to the exception message.
- Logging setup: `dev-stack:dotnet-observability`.

## Packages in the shared framework

Nine `Microsoft.Extensions.*` libraries are now in the shared framework: `Caching.Abstractions`, `Configuration.Abstractions`, `DependencyInjection.Abstractions`, `Diagnostics.Abstractions`, `FileProviders.Abstractions`, `Hosting.Abstractions`, `Logging.Abstractions`, `Options`, `Primitives`.

- A project that targets only `net11.0` and references one of them gets NU1510. Remove the `PackageReference` (and the `PackageVersion` in `Directory.Packages.props` when no other project needs it).
- A project with `<TargetFrameworks>net10.0;net11.0</TargetFrameworks>` gets no warning. Keep the reference.

## Upgrade steps

Do these steps only when the task asks for the upgrade (stack rule 1). The official `migrate-dotnet10-to-dotnet11` skill in github.com/dotnet/skills has the full procedure.

1. Change `TargetFramework` to `net11.0`, and update `global.json` to an 11.0 SDK.
2. Update the `Microsoft.AspNetCore.*` and `Microsoft.Extensions.*` packages to 11.0. Done when `dotnet restore` gives no NU1510 and no NU1605.
3. Build with `-warnaserror`. Fix each new obsolete warning.
4. Search for `UseConcurrencyLimiter`, `IAsyncValidateOptions`, OpenAPI transformers, and `[ProducesResponseType]` snapshot tests. Apply the sections of this file.
5. Run all tests, and one start of the host with the production configuration. Done when the tests pass and the host starts.
