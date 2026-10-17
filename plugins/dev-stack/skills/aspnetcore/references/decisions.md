# Team decisions: aspnetcore

The decisions that a team makes one time for new host and API code. Ask each decision when the work first needs it (stack rule 4). Existing code keeps its own style. Source: the defaults research of 2026-10-16. API-09 (OpenAPI) and API-10 (versioning) are in `dev-stack:aspnetcore-openapi`. CFG-04 (time source) is in `dev-stack:csharp`.

Batch the questions: ask API-01, API-17, API-03, API-11, API-04, API-05, and API-08 together as one "first endpoint" question. Ask the others one by one when the work needs them.

## Contents

- API-01. Minimal APIs or controllers
- API-17. URL style
- API-03. Request and response contracts
- API-11. JSON settings
- API-04. Error contract
- API-05. Expected failures: exceptions or result values
- API-08. Request validation
- API-02. Endpoint organization
- API-06. Mediator or direct calls
- API-07. Object mapping
- CFG-01. Options pattern and validation
- CFG-02. Secrets
- CFG-08. Authentication model
- CFG-09. Authorization model
- API-12. Pagination style
- API-16. Health endpoint paths
- CFG-06. HTTP resilience defaults
- CFG-05. Outbound HTTP clients
- API-15. CORS policy
- API-14. Rate limiting
- API-13. Idempotency for POST
- CFG-07. Background jobs
- CFG-03. Feature flags
- CFG-10. Localization of backend messages

## API-01. Minimal APIs or controllers

- Question: Do we write new endpoints as minimal APIs or as controllers?
- Ask when: the first endpoint.
- Options: Minimal APIs: `MapGet`, `MapGroup`, `TypedResults`. Controllers: `[ApiController]` classes.
- Recommended: Minimal APIs. Microsoft recommends them for new projects, `dotnet new webapi` creates them, and the built-in `AddValidation` works only for minimal APIs.
- Choose another option when: OData, MVC model binder or filter extensibility, application parts, or an existing controller code base.
- Depends on: none.
- Revisit when: never for new code.
- Check: judge (no mix of both styles in one project).
- Detect: ASP-DET-API-01

## API-17. URL style

- Question: Do routes use lowercase plural nouns with kebab-case segments, for example `/api/orders/{id}/line-items`?
- Ask when: the first endpoint.
- Options: Lowercase plural nouns, kebab-case, `/api` prefix, actions as `POST /api/orders/{id}/cancel`. camelCase or PascalCase segments. RPC verbs (`/api/cancelOrder`).
- Recommended: Lowercase plural nouns with kebab-case. Nouns name the resources, and an action that is not CRUD becomes a sub-resource with POST.
- Choose another option when: the organization has an API style guide.
- Depends on: API-01.
- Revisit when: never.
- Check: judge.
- Detect: none

## API-03. Request and response contracts

- Question: Do endpoints use their own request and response types, or return domain or EF entities?
- Ask when: the first endpoint with a body.
- Options: A `sealed record` per request and per response. One shared record per resource for request and response. Entities directly.
- Recommended: A `sealed record` per request and per response, in the feature folder. Entities cause over-posting, change the contract with each schema change, and can loop through navigation properties.
- Choose another option when: an internal admin tool can share one record per resource. Public endpoints use separate records.
- Depends on: API-01.
- Revisit when: never.
- Check: judge.
- Detect: none

## API-11. JSON settings

- Question: Do we keep the web JSON defaults and add enums as strings and strict number handling?
- Ask when: the first request or response type with an enum or a number.
- Options: Web defaults only (camelCase, case-insensitive read, numbers readable from strings, enums as numbers). Web defaults plus `JsonStringEnumConverter` and `NumberHandling = Strict`. Snake case (`JsonNamingPolicy.SnakeCaseLower`).
- Recommended: Web defaults plus `JsonStringEnumConverter` and `NumberHandling = Strict`, and `DateTimeOffset` for instants. String enums are readable, and strict numbers give `"type": "integer"` in the OpenAPI schema.
- Choose another option when: the clients expect snake case (Python or Ruby). An existing API keeps its settings, because a change breaks clients.
- Depends on: API-01.
- Revisit when: .NET 11 (`JsonSerializerDefaults.Strict`).
- Check: judge.
- Detect: ASP-DET-API-11

## API-04. Error contract

- Question: Do all errors use RFC 9457 ProblemDetails, with a stable machine-readable `code`?
- Ask when: the first response that is not 2xx.
- Options: ProblemDetails with an extension `code` and `traceId`. ProblemDetails with the framework defaults only. An own error envelope.
- Recommended: ProblemDetails everywhere, with `code` (for example `order.not_found`) on each 4xx and `traceId` from `CustomizeProblemDetails`. Clients act on the code and do not parse text.
- Choose another option when: the API must match an error contract that is already published.
- Depends on: API-01.
- Revisit when: never.
- Check: judge (`AddProblemDetails`, `UseExceptionHandler`, `UseStatusCodePages` in `Program.cs`).
- Detect: none

## API-05. Expected failures: exceptions or result values

- Question: For expected failures (not found, conflict, rule broken), do we return result values or throw exceptions, and do we use a result library?
- Ask when: the first handler that can fail for a business reason.
- Options: `TypedResults` unions at the endpoint and a small own result (or `null`, or a closed record set) from the handler. Custom exceptions mapped in one `IExceptionHandler`. ErrorOr. FluentResults, Ardalis.Result, or OneOf.
- Recommended: No library. Endpoints return `Results<...>` unions, handlers return a small own result, and exceptions are only for bugs and infrastructure failures. Plain status results with pattern matching cover most cases in idiomatic C#.
- Choose another option when: many layers pass errors up (ErrorOr, the only one of the four libraries with a release in the last 12 months). An existing code base that uses exceptions for flow keeps them, with one `IExceptionHandler` map.
- Depends on: API-04.
- Revisit when: the project targets a framework with C# union types (`net11.0` or later).
- Check: judge.
- Detect: ASP-DET-API-05

## API-08. Request validation

- Question: Do we validate requests with the built-in `AddValidation` (DataAnnotations), or with FluentValidation?
- Ask when: the first request type with a rule.
- Options: Built-in `builder.Services.AddValidation()` with DataAnnotations and `IValidatableObject`. FluentValidation with an explicit `ValidateAsync` call or a small endpoint filter. Manual checks in the handler.
- Recommended: FluentValidation when the project already uses it. Else the built-in `AddValidation` for shape rules (required, length, range, format). It is source-generated, needs no package, and writes a ProblemDetails 400.
- Choose another option when: many conditional or cross-field rules (FluentValidation). Controllers: the built-in validation does not apply, so use `[ApiController]` model validation or FluentValidation.
- Depends on: API-01, API-04.
- Revisit when: .NET 11 (async and localized built-in validation).
- Check: tool (ASP-FV-AUTOVALIDATION, ASP-PKG-FV-ASPNETCORE) and judge (one library per contract).
- Detect: ASP-DET-API-08

## API-02. Endpoint organization

- Question: Do we map endpoints with a plain `MapGroup` extension per feature, or with a library?
- Ask when: the second endpoint.
- Options: A static `MapX(this IEndpointRouteBuilder)` per feature with `MapGroup` and named static handler methods. One class per endpoint with an own small `IEndpoint` interface and an assembly scan. FastEndpoints. Carter.
- Recommended: A static `MapX` per feature with `MapGroup` and named static handler methods. It needs no package, keeps the standard OpenAPI metadata, and XML comments reach OpenAPI only from named methods.
- Choose another option when: the team wants one file per endpoint (own `IEndpoint`, still no library). FastEndpoints only when the team already knows it.
- Depends on: API-01.
- Revisit when: the mapping lines in `Program.cs` grow past one screen.
- Check: judge.
- Detect: ASP-DET-API-02

## API-06. Mediator or direct calls

- Question: Do endpoints call handler classes directly, or through a mediator library?
- Ask when: the first endpoint with logic beyond one query.
- Options: No mediator: the endpoint injects a handler class and calls it. Mediator (martinothamar, MIT, source-generated). Wolverine as mediator. MediatR is not allowed (team decision Q28a: license change since version 13).
- Recommended: No mediator. One handler class per use case, injected into the endpoint, and cross-cutting code in endpoint filters. A mediator hides the path from endpoint to handler.
- Choose another option when: the team wants in-process notifications and pipeline behaviors across many handlers (Mediator by martinothamar). The service already uses Wolverine for messaging (Wolverine).
- Depends on: API-01.
- Revisit when: the same cross-cutting code repeats in many endpoint filters.
- Check: tool (ASP-PKG-MEDIATR).
- Detect: ASP-DET-API-06

## API-07. Object mapping

- Question: Do we map between entities and response types by hand, or with Mapperly?
- Ask when: the first response type built from an entity.
- Options: Manual: a static `ToResponse()` method or a constructor. Mapperly (source generator, Apache-2.0). AutoMapper is not allowed (team decision Q28a: license change since version 15).
- Recommended: Manual mapping. The compiler checks it, a debugger can step through it, and a LINQ projection (`Select(x => new OrderResponse(...))`) lets EF read only the needed columns.
- Choose another option when: many large, flat mappings make manual code a real burden (Mapperly, which reports missing members at build time).
- Depends on: API-03.
- Revisit when: mapping code passes about 10% of a feature.
- Check: tool (ASP-PKG-AUTOMAPPER).
- Detect: ASP-DET-API-07

## CFG-01. Options pattern and validation

- Question: Do all settings bind to typed options classes that validate at startup?
- Ask when: the first setting read from configuration.
- Options: `AddOptions<T>().BindConfiguration(section).ValidateDataAnnotations().ValidateOnStart()`. The same with a source-generated `[OptionsValidator]`. FluentValidation for options. Raw `IConfiguration["key"]` reads.
- Recommended: Typed options with DataAnnotations and `ValidateOnStart`. Bad configuration then stops the start, not the first request.
- Choose another option when: native AOT or trimming (`[OptionsValidator]`). Complex cross-field option rules and FluentValidation already in the project.
- Depends on: none.
- Revisit when: .NET 11 (async startup validation).
- Check: tool (ASP-OPTIONS-NO-VALIDATE, ASP-OPTIONS-CONFIGURE-SECTION).
- Detect: ASP-DET-CFG-01

## CFG-02. Secrets

- Question: Where do secrets live in development and in production?
- Ask when: the first connection string, key, or password.
- Options: User-secrets in development, environment variables or orchestrator secrets in production. A vault configuration provider (Azure Key Vault, HashiCorp Vault) in production. `appsettings.{Environment}.json` (rejected).
- Recommended: User-secrets plus environment variables or orchestrator secrets. Add a vault provider when the platform already has one, and use a managed identity for it.
- Choose another option when: compliance asks for rotation and audit (vault).
- Depends on: CFG-01.
- Revisit when: the first secret rotation requirement.
- Check: judge (secret scanning is in `dev-stack:secure-coding`).
- Detect: none

## CFG-08. Authentication model

- Question: Which identity provider signs users in, and does a browser front-end use a cookie through a backend-for-frontend (BFF) or bearer tokens?
- Ask when: the first endpoint that needs a user.
- Options: API with JWT bearer from an external IdP (Keycloak, Entra ID), browser through a BFF with an HttpOnly cookie. API with JWT bearer, the SPA keeps the tokens in memory. Cookie auth with ASP.NET Core Identity in the same app. Own token issuing (rejected).
- Recommended: The API accepts JWT bearer from the IdP of the organization (Entra ID with Microsoft.Identity.Web, or Keycloak with `AddJwtBearer`). A browser SPA goes through a BFF with a cookie, because the browser does not expose an HttpOnly cookie to JavaScript.
- Choose another option when: one app with local users and no other clients (Identity with cookies). Machine-to-machine only (JWT bearer, client credentials).
- Depends on: none.
- Revisit when: a second client type (mobile, partner).
- License note: Duende.BFF needs a paid license in production. YARP is MIT.
- Check: judge (token validation rules are in `dev-stack:secure-coding`).
- Detect: ASP-DET-CFG-08

## CFG-09. Authorization model

- Question: Do we require an authenticated user by default (fallback policy) and express rules as named policies?
- Ask when: the first protected endpoint.
- Options: Fallback policy `RequireAuthenticatedUser`, named policies per permission, `AllowAnonymous` as the explicit exception. `RequireAuthorization()` on each group, roles in attributes. Checks inside handlers only.
- Recommended: Fallback policy "authenticated user", named policies mapped to scopes or roles from the IdP, resource checks in the handler through `IAuthorizationService`. A forgotten attribute then fails closed.
- Choose another option when: an API that is mostly anonymous (no fallback, protect the groups).
- Depends on: CFG-08.
- Revisit when: multi-tenant data (add a tenant requirement).
- Check: judge.
- Detect: ASP-DET-CFG-09

## API-12. Pagination style

- Question: Do list endpoints page by offset (page number and size) or by cursor (continuation token)?
- Ask when: the first list endpoint.
- Options: Offset with a maximum size. Cursor (keyset) with an opaque token. Both: offset for UI tables, cursor for exports and feeds.
- Recommended: Offset with a hard maximum page size (for example 100) and a unique, stable sort order. It fits page-number UIs and is simple.
- Choose another option when: large or fast-growing tables, infinite scroll, sync or export endpoints (cursor).
- Depends on: API-03.
- Revisit when: a list query passes about 100 ms on deep pages, or a table passes about 1 million rows.
- Check: judge (a maximum page size exists).
- Detect: none

## API-16. Health endpoint paths

- Question: Which paths do we use for liveness and readiness, and are they reachable in production?
- Ask when: the first deployment target (container or orchestrator).
- Options: `/healthz/live` (no checks) and `/healthz/ready` (tagged checks), mapped in all environments. Aspire defaults `/alive` and `/health` (mapped in Development only). One `/health` endpoint.
- Recommended: `/healthz/live` and `/healthz/ready`, mapped in all environments, short-circuited, and not routed through the public ingress. Liveness without dependency checks stops restart loops when a database is slow.
- Choose another option when: the platform prescribes the paths (for example App Service `/healthz`).
- Depends on: none.
- Revisit when: a new orchestrator.
- Check: judge.
- Detect: ASP-DET-API-16

## CFG-06. HTTP resilience defaults

- Question: Do all HTTP clients get the standard resilience handler by default, with retries off for unsafe methods?
- Ask when: the first outbound HTTP call.
- Options: `ConfigureHttpClientDefaults(b => b.AddStandardResilienceHandler())` with `DisableForUnsafeHttpMethods` where needed. The standard handler per client only. Polly 7 policies through Microsoft.Extensions.Http.Polly. None.
- Recommended: The standard handler on all clients through `ConfigureHttpClientDefaults`, and no retry for POST or PATCH unless the call is idempotent (API-13). One handler gives retries with jitter, a circuit breaker, and timeouts.
- Choose another option when: a client needs other timeouts (replace it per client with `RemoveAllResilienceHandlers`, which is experimental in 10.11.0).
- Depends on: none.
- Revisit when: never.
- Check: tool (ASP-PKG-HTTP-POLLY, ASP-POLLY-POLICY) and judge.
- Detect: ASP-DET-CFG-06

## CFG-05. Outbound HTTP clients

- Question: For calls to other HTTP APIs, do we write typed clients, use Refit, or generate a client from OpenAPI?
- Ask when: the first outbound HTTP call.
- Options: A typed client with `AddHttpClient<TClient>()`. A Refit interface with `AddRefitClient<T>()`. A generated client (Kiota or NSwag) from the OpenAPI document of the provider. A named client with `IHttpClientFactory.CreateClient`.
- Recommended: A typed client. It needs no package and gets the factory configuration and the resilience handler.
- Choose another option when: one external API with many endpoints and no good OpenAPI document (Refit). A provider with a reliable OpenAPI document (Kiota or NSwag, with the generated code committed). A singleton consumer (named client).
- Depends on: CFG-06.
- Revisit when: a typed client passes about 10 methods.
- Check: tool (ASP-NEW-HTTPCLIENT) and judge.
- Detect: ASP-DET-CFG-05

## API-15. CORS policy

- Question: Does the browser front-end come from the same origin as the API, or do we need CORS?
- Ask when: the first browser client.
- Options: No CORS: same origin through a BFF or reverse proxy. A named policy with exact origins from configuration. Any origin.
- Recommended: Same origin (no CORS) when CFG-08 selects a BFF. Else one named policy with exact origins from configuration.
- Choose another option when: a public read-only API without credentials (any origin).
- Depends on: CFG-08.
- Revisit when: a new front-end origin.
- Check: judge (CORS rules are in `dev-stack:secure-coding`).
- Detect: ASP-DET-API-15

## API-14. Rate limiting

- Question: Where do we rate limit: in the app, at the gateway, or both?
- Ask when: the first public or anonymous endpoint, or the first expensive endpoint (login, export, report, LLM call).
- Options: Built-in `AddRateLimiter` with named policies on sensitive endpoints, partitioned by user or IP. A global limiter on all endpoints. Gateway or ingress only.
- Recommended: The built-in limiter with `RejectionStatusCode = 429` and named policies on sensitive and anonymous endpoints only. The default 503 makes clients retry as for an outage.
- Choose another option when: a gateway with rate limits already fronts all traffic (global caps at the gateway, app policies for expensive endpoints).
- Depends on: CFG-08.
- Revisit when: scale-out to many replicas (the in-app limiter counts per instance).
- Check: judge.
- Detect: ASP-DET-API-14

## API-13. Idempotency for POST

- Question: Do create and action endpoints accept an idempotency key, so that a client can retry safely?
- Ask when: the first POST with a side effect outside the database (payment, mail, external call), or the first client with automatic retries.
- Options: Natural idempotency: the client sends the resource id and a unique constraint rejects duplicates. An `Idempotency-Key` header with a stored response per key (own endpoint filter and table). A library (IdempotentAPI, last release 2024). None.
- Recommended: Natural idempotency (a client id or a unique business key). Add an `Idempotency-Key` filter only on POSTs with external side effects. ASP.NET Core has no built-in feature, and the IETF header is still a draft.
- Choose another option when: a public API with payment-like operations (a key on each POST).
- Depends on: API-04.
- Revisit when: the IETF draft becomes an RFC.
- Check: judge.
- Detect: none

## CFG-07. Background jobs

- Question: For background work, do we use a plain `BackgroundService`, or a job library with persistence?
- Ask when: the first recurring or deferred task.
- Options: `BackgroundService` with `PeriodicTimer` (in memory, no persistence). Hangfire (persistent jobs, retries, dashboard, LGPL-3.0, paid Pro version). Quartz.NET (cron, calendars, clustering, Apache-2.0). TickerQ (newer, EF Core store). Wolverine scheduled messages or an orchestrator CronJob.
- Recommended: `BackgroundService` for in-process periodic work that a restart can lose. Hangfire when a job must survive a restart or needs retries and a dashboard. Jobs must be idempotent, because they can run twice.
- Choose another option when: cron calendars or clustered schedulers, or the organization bans LGPL (Quartz.NET). The service already uses Wolverine (scheduled messages). A job with no app state (Kubernetes CronJob).
- Depends on: none.
- Revisit when: the second persistent job type.
- License note: Hangfire is LGPL-3.0 for the open source part. Hangfire Pro needs a paid license.
- Check: judge.
- Detect: ASP-DET-CFG-07

## CFG-03. Feature flags

- Question: Do we use plain options, Microsoft.FeatureManagement, or the OpenFeature SDK for feature flags?
- Ask when: the first feature that must ship dark or roll out gradually.
- Options: Plain boolean options (`IOptionsMonitor<Features>`). Microsoft.FeatureManagement (MIT), flags in configuration. OpenFeature SDK (Apache-2.0) with a provider.
- Recommended: Plain options for a few static switches. Microsoft.FeatureManagement when targeting, percentage rollout, or runtime refresh is necessary.
- Choose another option when: the organization runs a flag service or has services in several languages (OpenFeature).
- Depends on: CFG-01.
- Revisit when: more than about 5 live flags, or the first targeted rollout.
- Check: judge.
- Detect: ASP-DET-CFG-03

## CFG-10. Localization of backend messages

- Question: Does the API return localized text, or stable error codes that the client translates?
- Ask when: the first user-facing message in a response.
- Options: A stable `code` plus an English `detail`, the client translates by code. Server localization with `IStringLocalizer` and `Accept-Language`. English only.
- Recommended: A stable `code` in ProblemDetails (API-04) plus an English `detail`. Localize on the server only text that the server renders (mail, PDF), or when no own UI exists.
- Choose another option when: third-party clients show the API text to end users directly (server localization with a supported culture list).
- Depends on: API-04.
- Revisit when: .NET 11 (localized built-in validation).
- Check: judge.
- Detect: none
