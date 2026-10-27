# Sources: aspnetcore

The author checked all facts on 2026-10-16, unless a line says otherwise. The author compiled the code with .NET SDK 10.0.402, `Microsoft.Extensions.Http.Resilience` 10.11.0, `Microsoft.AspNetCore.OpenApi` 10.0.13, `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.13, and `FluentValidation.DependencyInjectionExtensions` 12.2.0.

## Contents

- Versions and support
- Host, DI, options
- Endpoints, errors, validation
- HTTP and resilience
- Pipeline and wiring
- .NET 11
- Checked in this session
- Credits

## Versions and support

- .NET support policy (10.0.14 of 2026-10-25, LTS to 2028-11-14. .NET 8 and 9 end on 2026-11-10): https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- Release metadata: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- Breaking changes in .NET 10: https://learn.microsoft.com/dotnet/core/compatibility/10
- Breaking changes in ASP.NET Core 10: https://learn.microsoft.com/aspnet/core/breaking-changes/10/overview
- What is new in ASP.NET Core 10: https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-10.0

## Host, DI, options

- Generic Host and shutdown: https://learn.microsoft.com/aspnet/core/fundamentals/host/generic-host
- DI overview and scope validation: https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview
- Scoped services in a BackgroundService: https://learn.microsoft.com/dotnet/core/extensions/scoped-service
- BackgroundService change in .NET 10: https://learn.microsoft.com/dotnet/core/compatibility/extensions/10.0/backgroundservice-executeasync-task
- Options pattern: https://learn.microsoft.com/dotnet/core/extensions/options
- Options validation source generator: https://learn.microsoft.com/dotnet/core/extensions/options-validation-generator
- Null values in configuration (.NET 10): https://learn.microsoft.com/dotnet/core/compatibility/extensions/10.0/configuration-null-values-preserved
- Safe storage of app secrets: https://learn.microsoft.com/aspnet/core/security/app-secrets

## Endpoints, errors, validation

- APIs overview (minimal APIs or controllers): https://learn.microsoft.com/aspnet/core/fundamentals/apis
- Minimal API validation: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis#validation-support-in-minimal-apis
- Validation in ASP.NET Core: https://learn.microsoft.com/aspnet/core/fundamentals/validation
- Minimal API filters: https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/min-api-filters
- Error handling in APIs: https://learn.microsoft.com/aspnet/core/fundamentals/error-handling-api
- IExceptionHandler diagnostics change (.NET 10): https://learn.microsoft.com/aspnet/core/breaking-changes/10/exception-handler-diagnostics-suppressed
- JSON web defaults: https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/configure-options
- WithOpenApi deprecated (ASPDEPR002): https://learn.microsoft.com/aspnet/core/diagnostics/aspdepr002
- FluentValidation 12 and the deprecated ASP.NET Core package: https://github.com/FluentValidation/FluentValidation, https://www.nuget.org/packages/FluentValidation.AspNetCore
- Andrew Lock, "Is the result pattern worth it?" (2024-10-29): https://andrewlock.net/working-with-the-result-pattern-part-4-is-the-result-pattern-worth-it

## HTTP and resilience

- HTTP resilience: https://learn.microsoft.com/dotnet/core/resilience/http-resilience
- HttpClient guidelines: https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines
- IHttpClientFactory: https://learn.microsoft.com/dotnet/core/extensions/httpclient-factory
- HttpClient timeouts and TimeoutRejectedException: https://learn.microsoft.com/microsoft-cloud/dev/dev-proxy/concepts/dotnet-httpclient-timeouts
- HttpStandardResilienceOptions: https://learn.microsoft.com/dotnet/api/microsoft.extensions.http.resilience.httpstandardresilienceoptions
- Package data (versions, dependencies, licenses): https://api.nuget.org/v3-flatcontainer/

## Pipeline and wiring

- Middleware order: https://learn.microsoft.com/aspnet/core/fundamentals/middleware/
- Kestrel security considerations: https://learn.microsoft.com/aspnet/core/fundamentals/servers/kestrel/security-considerations
- KnownNetworks obsolete (ASPDEPR005): https://learn.microsoft.com/aspnet/core/breaking-changes/10/ipnetwork-knownnetworks-obsolete
- CORS: https://learn.microsoft.com/aspnet/core/security/cors
- Rate limiting: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- Policy-based authorization: https://learn.microsoft.com/aspnet/core/security/authorization/policies
- JWT bearer configuration: https://learn.microsoft.com/aspnet/core/security/authentication/configure-jwt-bearer-authentication
- Health checks: https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks
- File uploads: https://learn.microsoft.com/aspnet/core/mvc/models/file-uploads

## .NET 11

- What is new in ASP.NET Core 11: https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-11
- Breaking changes in ASP.NET Core 11: https://learn.microsoft.com/aspnet/core/breaking-changes/11/overview
- Breaking changes in .NET 11: https://learn.microsoft.com/dotnet/core/compatibility/11
- RunAsync throws on a failed BackgroundService: https://learn.microsoft.com/dotnet/core/compatibility/extensions/11/ihost-runasync-stopasync-throw-backgroundservice-failure
- Synchronous access to async-validated options throws: https://learn.microsoft.com/dotnet/core/compatibility/extensions/11/async-options-validation-sync-access
- Microsoft.Extensions packages in the shared framework: https://learn.microsoft.com/dotnet/core/compatibility/extensions/11/extensions-in-shared-framework
- ConcurrencyLimiter removed: https://learn.microsoft.com/aspnet/core/breaking-changes/11/concurrencylimiter-removed. The page names ASP0025 for the obsolete warning. That is wrong: ASP0025 is "Use AddAuthorizationBuilder". The `[Obsolete]` attribute on `UseConcurrencyLimiter` in the release/8.0 source has no diagnostic ID, so the warning is CS0618 (checked 2026-10-27).
- Kestrel protocol compliance: https://learn.microsoft.com/aspnet/core/breaking-changes/11/kestrel-strict-protocol-compliance
- Hosting OpenTelemetry tags: https://learn.microsoft.com/aspnet/core/breaking-changes/11/http-activity-otel-semconv
- Microsoft.OpenApi 3.x: https://learn.microsoft.com/aspnet/core/breaking-changes/11/microsoft-openapi-3x

## Checked in this session

The author checked these facts again with a build or a run on 2026-10-16:

- The template builds with `-warnaserror` and `AnalysisLevel=latest-recommended`.
- A missing `Payment:BaseUrl` stops the start with `OptionsValidationException` (`ValidateOnStart`).
- With the fallback policy, `/healthz/live` returns 200 without a token, and an unknown path returns a 401 `application/problem+json` body with `traceId`.
- `AddValidation` validates positional record parameters with and without the `property:` target.
- A ternary of two `TypedResults` in a lambda without a declared return type fails with CS1678 and CS1661.
- `BuildServiceProvider()` in `Program.cs` gives ASP0000. The same call in an `AddX` extension method gives no warning.
- `.WithOpenApi()` gives ASPDEPR002, and `WebHost` gives ASPDEPR008, both as warnings in a default build.
- `DisableForUnsafeHttpMethods()` needs `using Microsoft.Extensions.Http.Resilience;` and gives no EXTEXP0001 diagnostic with version 10.11.0. The research said it was experimental: that is not true for 10.11.0.
- `RemoveAllResilienceHandlers()` gives EXTEXP0001 as an error with version 10.11.0.
- A client with a handler from `ConfigureHttpClientDefaults` and a second `AddStandardResilienceHandler` made 16 attempts against an always-failing handler. With `RemoveAllResilienceHandlers()` first, or with one handler, it made 4.
- The .NET 11 facts come from the RC 2 documentation pages listed in the ".NET 11" section.

## Checked on 2026-10-27

- .NET 10.0.14 and SDK 10.0.403 (2026-10-25, security release), .NET 11 RC 3: release metadata https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json and the 11.0 file.
- `WebHostBuilder` gives ASPDEPR004, `WebHost` and `IWebHost` give ASPDEPR008: https://learn.microsoft.com/aspnet/core/breaking-changes/10/webhostbuilder-deprecated
- ASP.NET Core supports a `Startup` class, but Microsoft does not recommend it: https://learn.microsoft.com/aspnet/core/fundamentals/startup
- On nuget.org: Microsoft.AspNetCore.OpenApi, Microsoft.AspNetCore.Authentication.JwtBearer, and Microsoft.Extensions.Http.Polly 10.0.14. Microsoft.Extensions.Http.Resilience 10.11.0 and FluentValidation.DependencyInjectionExtensions 12.2.0 are still the latest.

## Credits

- github.com/dotnet/skills, skill `dotnet-webapi` (MIT, .NET team): the base for the endpoint rules. https://github.com/dotnet/skills. This skill paraphrases these parts:
  - `TypedResults` with a declared `Results<...>` type, and the ternary compile trap.
  - The status code table.
  - `sealed record` request and response types, and `DateTimeOffset` in contracts.
  - `AddValidation` for .NET 10 minimal APIs.
  - No Swashbuckle in new .NET 9+ projects.
  - An `IExceptionHandler` that logs before it returns `true`.
  - Strict JSON settings only for new projects.
- Not taken from `dotnet-webapi`: "an interface for each service" and "exception handlers in a `Middleware/` folder".
- The earlier own skill `dotnet-modern`: this skill includes its DI and options parts. Its language parts moved to `dev-stack:csharp`.
