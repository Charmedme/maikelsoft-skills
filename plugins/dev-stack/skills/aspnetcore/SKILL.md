---
name: aspnetcore
description: Host-level .NET code for ASP.NET Core 10 and worker services. Covers the Generic Host, DI lifetimes, options, middleware order, minimal API endpoints, ProblemDetails, typed HTTP clients with resilience, health checks, background services, and the wiring of CORS, rate limits, and authentication. Use when the user writes or reviews Program.cs, endpoints, controllers, middleware, DI registrations, options classes, HttpClient code, BackgroundService or IHostedService, or health checks in a project with Microsoft.NET.Sdk.Web or Microsoft.Extensions.Hosting. Not for C# language rules (dev-stack:csharp), security rules (dev-stack:secure-coding), OpenAPI and versioning detail (dev-stack:aspnetcore-openapi), FluentValidation rules (dev-stack:fluentvalidation), EF Core (dev-stack:efcore), logging setup (dev-stack:dotnet-observability), or Aspire AppHost wiring.
metadata:
  verified: "2026-10-16"
  versions: ".NET 10 (10.0.13, SDK 10.0.402), ASP.NET Core 10, Microsoft.Extensions.Http.Resilience 10.11.0, .NET 11 RC 2 in references/net11.md"
---

# ASP.NET Core and the Generic Host

This skill covers code that needs a host: `IServiceCollection`, `IHost`, `HttpContext`, or an endpoint. Code that compiles in a plain class library is `dev-stack:csharp`, and this skill does not repeat its rules. Security rules are in `dev-stack:secure-coding`. This skill only shows where the security parts go in the pipeline.

## First

1. Read `references/stack-rules.md`.
2. Read the version gate:
   - Read `TargetFramework` in the `*.csproj` files and in `Directory.Build.props`, and the `Sdk` attribute.
   - `net10.0`: use this file.
   - `net11.0`: also read `references/net11.md`. Its rules apply only when the project targets `net11.0`.
   - `net8.0` or `net9.0`: the rules apply, except `AddValidation` (rule 24) and the .NET 10 behavior changes (rules 9, 22, 34). Both versions end support on 2026-11-10. Tell the human.
   - A ServiceDefaults project (Aspire) exists: it already adds resilience, health checks, and OpenTelemetry. Do not add them a second time. Use the `aspire` skill for that project.
3. Read `Program.cs` and one existing feature before you write. The project style wins in existing code (stack rule 3).

Done when you can name the target framework, the endpoint style (minimal APIs or controllers), the validation library, and the error style of the project.

## Defaults for new code

Existing code keeps its style. For new code, use the accepted decision in `docs/dev-team/decisions.md`. When a decision has no accepted answer, ask the human (stack rule 4). The detail of each decision is in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Endpoints | Minimal APIs | API-01 |
| Organization | Static `MapX` per feature, `MapGroup`, named static handler methods | API-02 |
| Contracts | `sealed record` per request and per response, no entities | API-03 |
| Errors | ProblemDetails everywhere, extension `code`, `traceId` | API-04 |
| Expected failures | `TypedResults` union, no result library | API-05 |
| Handlers | Direct handler class, no mediator (MediatR is not allowed) | API-06 |
| Mapping | Manual, Mapperly for large mappings (AutoMapper is not allowed) | API-07 |
| Validation | `AddValidation` for shape rules, FluentValidation when the project uses it | API-08 |
| JSON | Web defaults, `JsonStringEnumConverter`, `NumberHandling.Strict`, `DateTimeOffset` | API-11 |
| Paging | Offset with a maximum page size and a stable sort | API-12 |
| Idempotency | Natural (client id or unique key), key header only for external side effects | API-13 |
| Rate limiting | Built-in limiter, 429, named policies on sensitive endpoints | API-14 |
| CORS | Same origin, else one named policy with exact origins | API-15 |
| Health | `/healthz/live` and `/healthz/ready` | API-16 |
| URLs | `/api/orders/{id}/line-items`, actions as `POST /api/orders/{id}/cancel` | API-17 |
| Options | Typed options, DataAnnotations, `ValidateOnStart` | CFG-01 |
| Secrets | User-secrets in Development, environment or orchestrator secrets in production | CFG-02 |
| Feature flags | Plain options, Microsoft.FeatureManagement for rollouts | CFG-03 |
| Outbound HTTP | Typed client | CFG-05 |
| Resilience | Standard handler on all clients, no retry for unsafe methods | CFG-06 |
| Background work | `BackgroundService`, Hangfire when a job must survive a restart | CFG-07 |
| Authentication | JWT bearer from the IdP of the organization, browser SPA through a BFF | CFG-08 |
| Authorization | Fallback policy "authenticated user", named policies | CFG-09 |
| Messages | Stable `code` plus English `detail`, the client translates | CFG-10 |

## Rules

### Host and DI

1. Use `WebApplication.CreateBuilder` for web apps and `Host.CreateApplicationBuilder` for workers. `WebHost` and `Startup` classes are obsolete in .NET 10.
2. Register the services of a feature in `AddX(this IServiceCollection)` and map its endpoints in `MapX(this IEndpointRouteBuilder)`. `Program.cs` then stays short.
3. Turn on `ValidateScopes` and `ValidateOnBuild` in each environment with `builder.Host.UseDefaultServiceProvider`. Production turns them off by default, so a captive dependency shows only under load.
4. A singleton or a hosted service does not take a scoped service. Create a scope per unit of work with `IServiceScopeFactory.CreateAsyncScope()`.
5. Do not call `BuildServiceProvider()` during registration. It makes a second container with its own singletons. Use the `(sp, ...) =>` factory overloads or `IOptions<T>`.

### Configuration and options

6. Bind each settings section to an options class: `AddOptions<T>().BindConfiguration(section).ValidateDataAnnotations().ValidateOnStart()`. Bad configuration then stops the start, not the first request.
7. Mark nested objects with `[ValidateObjectMembers]` and lists with `[ValidateEnumeratedItems]`. Without them, the validator skips the members. For trimming or native AOT, use a source-generated `[OptionsValidator]`.
8. Read `IOptions<T>` for values that do not change, `IOptionsMonitor<T>` in a singleton that must see changes. Use `IOptionsSnapshot<T>` only in scoped services.
9. .NET 10: a JSON `null` in configuration now binds as `null` (or `default(T)` for a value type). It no longer keeps the default value of the class. Remove a `null` entry that means "use the default", and validate required values.
10. Keep secrets out of `appsettings.json`. Read them from user-secrets in Development and from environment variables, orchestrator secrets, or a vault in production.

### Pipeline order

11. Add middleware in this order: `UseForwardedHeaders` (behind a proxy), `UseExceptionHandler`, `UseStatusCodePages`, `UseHsts`, `UseHttpsRedirection`, `UseCors`, `UseAuthentication`, `UseAuthorization`, `UseRateLimiter`, then the endpoints. The order is the behavior: a wrong order gives a wrong client IP, failed CORS preflights, or unprotected endpoints.

### Endpoints

12. Write new endpoints as minimal APIs. In a project with controllers, add controllers. Do not mix the two styles in one project.
13. Return `TypedResults`. Declare more than one outcome with a `Results<T1, T2>` return type. A ternary with `TypedResults` and no declared return type does not compile (CS1678, CS1661).
14. Take a `CancellationToken` in each handler and pass it to each I/O call (CA2016).
15. A create returns 201 with a `Location` header (`TypedResults.Created`, `CreatedAtRoute`). A delete returns 204.
16. Use `sealed record` types for requests and responses, and `DateTimeOffset` for instants. Do not expose EF entities, because they leak fields and change the contract with each schema change.
17. Call handler classes directly and map with a static `ToResponse()` method or a constructor. MediatR and AutoMapper are not allowed (stack rule 5).
18. Configure JSON with `ConfigureHttpJsonOptions` for minimal APIs and `AddJsonOptions` for controllers. They are two different option objects. In an existing API, keep the JSON settings, because a change breaks clients.

### Errors

19. Register `AddProblemDetails()`, `UseExceptionHandler()`, and `UseStatusCodePages()`. Then each error is `application/problem+json`, and endpoints need no try/catch.
20. Return expected failures as results: `TypedResults.NotFound()`, or `TypedResults.Problem` with status 409 and a `code` extension. Throw exceptions only for bugs and infrastructure failures.
21. Map infrastructure exceptions to a status code in one `IExceptionHandler`, or with `ExceptionHandlerOptions.StatusCodeSelector`.
22. .NET 10: when `TryHandleAsync` returns `true`, the middleware does not log the exception. It also does not add `error.type` to the metric. Log in the handler, or set `SuppressDiagnosticsCallback = _ => false`.
23. Do not put `exception.Message` or a stack trace in a response. Do not call `UseDeveloperExceptionPage()`: `WebApplication` turns it on in Development.

### Validation

24. .NET 10 minimal APIs: call `builder.Services.AddValidation()` and put DataAnnotations on the request records. Invalid input then gets a 400 ProblemDetails. The source generator sees only the types of the assembly that calls `AddValidation`.
25. When the project uses FluentValidation: inject `IValidator<T>`, call `ValidateAsync` with the token, and return `TypedResults.ValidationProblem(result.ToDictionary())`. For minimal APIs, use one small generic endpoint filter (`references/endpoints-errors.md`). Do not use the package `FluentValidation.AspNetCore`: its owner deprecated it. Rules for validators: `dev-stack:fluentvalidation`.
26. Validate one contract with one library. Two libraries give two error formats.

### Outbound HTTP

27. Use typed clients from `AddHttpClient<TClient>()`. Do not create `new HttpClient()` per call, because it exhausts sockets.
28. Do not inject a typed client into a singleton. The singleton keeps one handler and does not see DNS changes. In a singleton, inject `IHttpClientFactory` and create a named client per call.
29. Add one resilience handler per client: `ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler())`. A second handler on a client stacks, and a test counted 16 attempts, not 4 attempts. To change one client, first call `RemoveAllResilienceHandlers()` (experimental, EXTEXP0001). Then add the handler of the client.
30. Turn off retries for calls that are not idempotent: `options.Retry.DisableForUnsafeHttpMethods()`. The standard handler retries POST by default and can write twice.
31. Keep `HttpClient.Timeout` (100 s default) above the total timeout of the pipeline (30 s default), or set `Timeout.InfiniteTimeSpan`.
32. Do not add `Microsoft.Extensions.Http.Polly` to new code. It pins Polly 7. Use `Microsoft.Extensions.Http.Resilience`.

### Health, background work, shutdown

33. Liveness runs no checks (`Predicate = _ => false`). Readiness runs the checks with the tag `ready`. Mark both `AllowAnonymous()` and `ShortCircuit()`, and keep them off the public ingress.
34. .NET 10: `ExecuteAsync` runs fully on a background thread, so code before the first `await` no longer delays the start. Put work that must finish before the app takes traffic in `StartAsync`.
35. In a `BackgroundService` loop, use `PeriodicTimer(period, timeProvider)`, one scope per iteration, and a catch per iteration that lets `OperationCanceledException` pass. An unhandled exception stops the host.
36. Pass `stoppingToken` to each call. Keep shutdown work under `HostOptions.ShutdownTimeout` (30 s default) and under the grace period of the orchestrator.
37. For work that starts in a request and ends later, write to a `Channel<T>` that a `BackgroundService` reads. `Task.Run` from a request loses the work on shutdown and loses the request scope.

### Cross-cutting wiring

The security rules for these parts are in `dev-stack:secure-coding`. This skill gives the place in the pipeline.

38. Behind a proxy, configure `KnownIPNetworks` or `KnownProxies` and call `UseForwardedHeaders` first. `KnownNetworks` is obsolete (ASPDEPR005).
39. CORS: one named policy with exact origins from configuration. Call `UseCors` before `UseAuthorization`.
40. Authorization: set a fallback policy `RequireAuthenticatedUser()`, use named policies, and mark public endpoints `AllowAnonymous()`. A forgotten attribute then fails closed.
41. Rate limiting: `AddRateLimiter` with `RejectionStatusCode = 429` (the default is 503), and `RequireRateLimiting` on login, export, upload, and expensive endpoints.
42. File uploads: follow `dev-stack:secure-coding` (file signatures, size limits, storage).

### OpenAPI and versioning

43. Use `AddOpenApi()` and `MapOpenApi()` in Development. Do not add Swashbuckle to a new project (API-09). Do not call `.WithOpenApi()` (ASPDEPR002). Declare each status code with `Results<...>` and `.ProducesProblem(status)`. Detail, Scalar, and versioning: `dev-stack:aspnetcore-openapi`.

## Never write

| Wrong | Correct |
|-------|---------|
| `Startup.cs`, `WebHost.CreateDefaultBuilder(args)` | `WebApplication.CreateBuilder(args)` in `Program.cs` |
| `services.BuildServiceProvider()` to read a setting | `(sp, x) => sp.GetRequiredService<IOptions<T>>().Value` |
| `services.Configure<T>(config.GetSection("X"))` without validation | `AddOptions<T>().BindConfiguration("X").ValidateDataAnnotations().ValidateOnStart()` |
| `configuration["Payment:Url"]` in a service | Inject `IOptions<PaymentOptions>` |
| Scoped `DbContext` in a singleton or `BackgroundService` constructor | `IServiceScopeFactory.CreateAsyncScope()` per unit of work |
| `Results.Ok(x)` and `IResult` everywhere | `TypedResults.Ok(x)` with `Results<Ok<T>, NotFound>` |
| `try { } catch (Exception ex) { return StatusCode(500, ex.Message); }` | `UseExceptionHandler()` and an `IExceptionHandler` |
| `Detail = exception.Message` | A fixed title and a stable `code` |
| Own exception middleware class | `IExceptionHandler` or `StatusCodeSelector` |
| `app.UseDeveloperExceptionPage();` | Nothing: Development has it by default |
| `AddFluentValidation()`, `AddFluentValidationAutoValidation()` | `ValidateAsync` in an endpoint filter |
| `using var client = new HttpClient();` in a method | Typed client from `AddHttpClient<T>()` |
| `.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, ...))` | `.AddStandardResilienceHandler()` |
| `services.AddStandardResilienceHandler()` | It extends `IHttpClientBuilder`: `AddHttpClient<T>().AddStandardResilienceHandler()` |
| Two resilience handlers on one client | One handler, or `RemoveAllResilienceHandlers()` first |
| `Task.Run(() => SendMailAsync())` in a handler | `Channel<T>` and a `BackgroundService` |
| `while (true) { ...; await Task.Delay(10000); }` | `PeriodicTimer` with `TimeProvider` and `stoppingToken` |
| `AddSwaggerGen()` and `.WithOpenApi()` in a new .NET 10 API | `AddOpenApi()`, `MapOpenApi()` |
| `KnownNetworks.Add(new IPNetwork(...))` | `KnownIPNetworks.Add(IPNetwork.Parse("10.0.0.0/8"))` |
| `AllowAnyOrigin()` with `AllowCredentials()` | `WithOrigins(origins)` from configuration |
| `public partial class Program { }` for tests | Not needed in .NET 10 |
| MediatR `ISender.Send(...)`, AutoMapper `IMapper.Map(...)` | Handler class, manual mapping or Mapperly |

## Check

Run the tools in this order. Fix each error. For each warning, fix it or give the reason.

1. Build with the analyzers. Pass: 0 warnings and 0 errors.

   ```
   dotnet build -warnaserror
   ```

2. Check the format. Pass: exit code 0.

   ```
   dotnet format --verify-no-changes
   ```

3. Run the rule check of this skill. Pass: `stackcheck: 0 errors`. In an existing project, add `--changed <base branch>`.

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

4. Find deprecated and vulnerable packages. Pass: no rows.

   ```
   dotnet list package --deprecated --vulnerable --include-transitive
   ```

5. Run the tests. Pass: all green, including one test that starts the host with `ValidateOnBuild` and one HTTP test per new endpoint (`dev-stack:dotnet-testing`).

   ```
   dotnet test
   ```

## Review checklist

The full list is in `references/review-checklist.md`.

1. `Program.cs` uses `WebApplication.CreateBuilder` or `Host.CreateApplicationBuilder`. tool (ASPDEPR008, ASP-WEBHOST)
2. No `BuildServiceProvider()` during registration. tool (ASP0000, ASP-BUILD-PROVIDER)
3. No scoped service in a singleton or hosted service, and `ValidateOnBuild` is on. judge
4. Each options class binds a section, validates it, and uses `ValidateOnStart`. tool (ASP-OPTIONS-NO-VALIDATE, ASP-OPTIONS-CONFIGURE-SECTION)
5. Middleware order matches rule 11. tool (ASP-ORDER-CORS, ASP-ORDER-AUTH, ASP-ORDER-FORWARDED) and judge
6. Handlers return `TypedResults` with a declared `Results<...>` type. tool (ASP-RESULTS-UNTYPED)
7. Each handler takes a `CancellationToken` and passes it on. tool (CA2016) and judge
8. Requests and responses are records, not entities. judge
9. Errors are ProblemDetails, and no response contains an exception message. tool (ASP-EXCEPTION-MESSAGE) and judge
10. An `IExceptionHandler` that returns `true` logs the exception. judge
11. No `new HttpClient(`, no typed client in a singleton. tool (ASP-NEW-HTTPCLIENT) and judge
12. One resilience handler per client, no retry for unsafe methods, no Polly 7. tool (ASP-PKG-HTTP-POLLY, ASP-POLLY-POLICY) and judge
13. Liveness has no checks, readiness has the `ready` checks. judge
14. A `BackgroundService` uses a scope and a catch per iteration and honors `stoppingToken`. judge
15. No deprecated FluentValidation integration, no MediatR, no AutoMapper. tool (ASP-PKG-FV-ASPNETCORE, ASP-FV-AUTOVALIDATION, ASP-PKG-MEDIATR, ASP-PKG-AUTOMAPPER)

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/program-template.md`: read before you write a new `Program.cs`, endpoint group, typed client, exception handler, background service, or worker. All code compiled.
- `references/hosting-di-options.md`: read when you register services, bind options, or write a hosted service.
- `references/endpoints-errors.md`: read when you write an endpoint, a request or response type, error handling, validation, or JSON settings.
- `references/http-resilience.md`: read when the code calls another HTTP API.
- `references/pipeline-wiring.md`: read when you add forwarded headers, CORS, authentication, authorization, rate limiting, or health endpoints.
- `references/net11.md`: read only when the project targets `net11.0`.
- `references/decisions.md`: read when new code needs a decision from the defaults table.
- `references/review-checklist.md`: read when you review a change.
- `references/sources.md`: the primary sources and the date of each check.
- `references/stack-rules.md`: the common rules of each dev-stack skill.
- `references/language.md`: the rules for text output.
