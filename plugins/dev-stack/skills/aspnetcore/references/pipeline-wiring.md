# Pipeline order and cross-cutting wiring

Where each part goes in the request pipeline: forwarded headers, HTTPS, CORS, authentication, authorization, rate limiting, health checks. The security rules for each part (token validation, CORS origins, antiforgery, headers, uploads) are in `dev-stack:secure-coding`. Each code block compiled with .NET SDK 10.0.402.

## Contents

- Pipeline order
- Forwarded headers
- HTTPS and HSTS
- CORS
- Authentication
- Authorization
- Rate limiting
- Health checks
- Short-circuit endpoints
- File uploads
- Logging and telemetry

## Pipeline order

Middleware runs in the order of the `Use...` calls. The template order:

```csharp
app.UseForwardedHeaders();      // 1. behind a proxy: real scheme and client IP first
app.UseExceptionHandler();      // 2. catches the exceptions of all later middleware
app.UseStatusCodePages();       // 3. ProblemDetails body for empty error responses
app.UseHsts();                  // 4. not in Development
app.UseHttpsRedirection();      // 5.
app.UseCors("web");             // 6. before authentication, so preflights pass
app.UseAuthentication();        // 7.
app.UseAuthorization();         // 8.
app.UseRateLimiter();           // 9. after authentication, so policies can partition by user
// endpoints                    // 10. MapX(), MapHealthChecks(), MapOpenApi()
```

`WebApplication` adds `UseRouting` at the start and `UseEndpoints` at the end. Call `UseRouting` yourself only when a middleware must run before routing.

What breaks with a wrong order:

| Wrong order | Effect |
|-------------|--------|
| `UseForwardedHeaders` late | HTTPS redirect loops, wrong client IP in rate limits and logs |
| `UseExceptionHandler` late | Exceptions in earlier middleware give an empty 500 |
| `UseCors` after `UseAuthorization` | Preflight requests fail |
| `UseAuthorization` before `UseAuthentication` | No user, so each protected endpoint gives 401 |
| `UseRateLimiter` before `UseAuthentication` | A per-user policy sees no user |

`ASP-ORDER-CORS`, `ASP-ORDER-AUTH`, and `ASP-ORDER-FORWARDED` find these three errors in one file.

## Forwarded headers

Behind a reverse proxy (ingress, load balancer), the app sees the address of the proxy and `http`. `UseForwardedHeaders` reads `X-Forwarded-For` and `X-Forwarded-Proto`, but only from known proxies:

```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
});
```

- `KnownNetworks` and `Microsoft.AspNetCore.HttpOverrides.IPNetwork` are obsolete in .NET 10 (ASPDEPR005). Use `KnownIPNetworks` with `System.Net.IPNetwork`.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` clears the lists and trusts each sender. A client can then fake its IP.

## HTTPS and HSTS

- `UseHsts()` outside Development only. The default `max-age` is 30 days.
- An API can reject HTTP instead of a redirect, because API clients do not follow a redirect for a POST body.
- When TLS ends at the ingress, `UseHttpsRedirection` needs forwarded headers first, else it loops.

## CORS

Only for a browser front-end on another origin (decision API-15). Same origin through a BFF needs no CORS.

```csharp
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("web", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));
```

- One named policy, exact origins from configuration.
- `AllowAnyOrigin()` with `AllowCredentials()` is invalid, and `SetIsOriginAllowed(_ => true)` with credentials is insecure.
- CORS is not authorization. It does not stop a client outside a browser.

## Authentication

Decision CFG-08 selects the model. Wiring for JWT bearer:

```csharp
builder.Services.AddAuthentication().AddJwtBearer();
```

- With one scheme, `AddAuthentication()` without a default scheme works: the single scheme is the default.
- Put the authority, the audience, and the valid issuers in configuration (`Authentication:Schemes:Bearer`), not in code.
- The validation rules (issuer, audience, lifetime, algorithms, `MapInboundClaims`) are in `dev-stack:secure-coding`.
- Since .NET 10, cookie authentication returns 401 and 403 for known API endpoints, not a redirect to a login page. Code that expected a 302 breaks.
- Package `Microsoft.AspNetCore.Authentication.JwtBearer`. It is not in the shared framework (ask the human, stack rule 5).

## Authorization

Decision CFG-09:

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("orders:write", policy => policy.RequireClaim("scope", "orders.write"));
```

- The fallback policy protects each endpoint without an own policy. A forgotten attribute then fails closed.
- Mark public endpoints `AllowAnonymous()`: health, the OpenAPI document in Development, a login callback.
- Use a named policy per permission: `.RequireAuthorization("orders:write")`.
- Check access to one resource (this order belongs to this customer) in the handler with `IAuthorizationService.AuthorizeAsync(user, resource, policy)`.

## Rate limiting

Decision API-14:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-user", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});
```

- Set `RejectionStatusCode = 429`. The default 503 makes clients retry as for an outage.
- Partition by user, else by client IP. The IP is correct only after forwarded headers.
- Put `.RequireRateLimiting("policy")` on login, password reset, export, upload, report, and LLM endpoints.
- The limiter counts per instance. A global cap over replicas belongs at the gateway.

## Health checks

Decision API-16:

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<PaymentApiHealthCheck>("payment-api", tags: ["ready"]);

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false }).AllowAnonymous().ShortCircuit();
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous().ShortCircuit();
```

- Liveness runs no checks. It answers "the process runs". A dependency check on liveness makes the orchestrator restart all pods when the database is slow.
- Readiness runs the checks with the tag `ready`. It answers "send me traffic".
- Keep each check cheap, with a short timeout. A health check runs every few seconds.
- With a fallback policy, mark both endpoints `AllowAnonymous()`. A run of the template returned 200 on `/healthz/live` without a token.
- Keep them off the public ingress, or bind them to an internal port with `RequireHost`:

```csharp
namespace Api.Snippets;

public static class HealthSetup
{
    // Health endpoints on an internal port only, so the public ingress cannot reach them.
    public static void MapInternalHealth(this WebApplication app)
    {
        app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false })
            .RequireHost("*:8081").AllowAnonymous().ShortCircuit();
        app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") })
            .RequireHost("*:8081").AllowAnonymous().ShortCircuit();
    }
}
```

The probe settings for Kubernetes are in `dev-stack:kubernetes`.

## Short-circuit endpoints

`.ShortCircuit()` runs the endpoint right after routing and skips the later middleware (authentication, CORS, rate limiting). Use it for health checks and `robots.txt`. Do not use it on an endpoint that needs a user. .NET 11 adds the attribute form `[ShortCircuit]`.

## File uploads

Size limits, file signatures, storage outside the web root, and malware scans are in `dev-stack:secure-coding`. The host parts:

- Kestrel `MaxRequestBodySize` is 30,000,000 bytes by default. Set a smaller limit per endpoint.
- Stream large files. Do not copy a whole upload into a `MemoryStream`.
- Put a rate limit policy on the upload endpoint.

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Api.Snippets;

public static class UploadSetup
{
    // A smaller body limit for one endpoint than the Kestrel default (30,000,000 bytes).
    public static void MapUpload(this IEndpointRouteBuilder app) =>
        app.MapPost("/api/files", (IFormFile file) => TypedResults.Accepted((string?)null))
            .WithMetadata(new RequestSizeLimitAttribute(5_000_000))
            .RequireRateLimiting("per-user");
}
```

## Logging and telemetry

The logging pipeline (ILogger, `[LoggerMessage]`, Serilog, OpenTelemetry, Application Insights) is in `dev-stack:dotnet-observability`. The host rules here:

- Log with `[LoggerMessage]` methods, as in the template.
- An `IExceptionHandler` that returns `true` must log the exception (.NET 10 change, `references/endpoints-errors.md`).
- An Aspire ServiceDefaults project already adds OpenTelemetry, health checks, and resilience. Do not add them a second time.
