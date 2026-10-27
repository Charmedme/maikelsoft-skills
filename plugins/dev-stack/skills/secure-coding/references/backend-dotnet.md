# Backend security for ASP.NET Core 10

The rules and the compiled code for the API side. The code was compiled with .NET SDK 10.0.402 and `AnalysisModeSecurity=All`, with 0 security warnings. Pipeline order is in `dev-stack:aspnetcore`. EF Core raw SQL is in `dev-stack:efcore`.

## Contents

- Authentication with JWT bearer
- Authorization and tenant isolation
- Overposting and exposure
- Cookies, BFF and antiforgery
- CORS
- Data Protection
- Input validation and output encoding
- SQL with EF Core and Dapper
- Deserialization
- XML
- SSRF
- Paths and redirects
- Regex and ReDoS
- Commands
- Errors
- Logging
- Crypto and random values
- Rate limits and size limits
- Headers, HTTPS and proxies
- Version notes

## Authentication with JWT bearer

Let JwtBearer validate the token. It checks issuer, audience, lifetime and signature when `Authority` and `Audience` are set. Default `ClockSkew` is 5 minutes. `MapInboundClaims` is `true` by default and renames `sub` and `roles`.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = builder.Configuration["Auth:Authority"]; // HTTPS: metadata and signing keys
        o.Audience = builder.Configuration["Auth:Audience"];   // one audience per API
        o.MapInboundClaims = false;                            // keep "sub", "roles" as sent
        o.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.RsaSha256];
        o.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(1);
    });
```

- Do not set `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` or `RequireSignedTokens` to `false`. The reason to turn one off is almost always a wrong `Authority` or `Audience`. Fix that.
- `RequireHttpsMetadata = false` only for a local identity provider, behind `IsDevelopment()`.
- `JwtSecurityTokenHandler.ReadJwtToken` does not validate. For manual validation, use `JsonWebTokenHandler.ValidateTokenAsync`. The sync `ValidateToken` is obsolete.
- Keycloak issues the tokens. This skill covers the validation in the API.

## Authorization and tenant isolation

A fallback policy closes each endpoint that has no policy. Named policies keep the rules in one place.

```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy(Permissions.ShipmentsRead, p => p.RequireClaim("permission", Permissions.ShipmentsRead))
    .AddPolicy(Permissions.ShipmentsWrite, p => p.RequireClaim("permission", Permissions.ShipmentsWrite));
builder.Services.AddSingleton<IAuthorizationHandler, ShipmentOwnerHandler>();
```

Read the tenant from the validated token, and apply it in one place with a global query filter.

```csharp
public sealed class ClaimsTenantContext(IHttpContextAccessor http) : ITenantContext
{
    public Guid TenantId =>
        Guid.TryParse(http.HttpContext?.User.FindFirstValue("tenant_id"), out var id)
            ? id
            : throw new UnauthorizedAccessException("The token has no tenant.");
}

// In AppDbContext.OnModelCreating:
b.Entity<Shipment>().HasQueryFilter(s => s.TenantId == tenant.TenantId);
```

The query filter does not cover Dapper, raw SQL that you do not compose with LINQ, `IgnoreQueryFilters()`, or other stores. Add the tenant condition there yourself.

An object of another tenant gives 404. The API then does not confirm that the id exists.

```csharp
private static async Task<Results<Ok<ShipmentResponse>, NotFound>> GetAsync(
    Guid id, AppDbContext db, CancellationToken ct)
{
    var shipment = await db.Shipments
        .Where(s => s.Id == id)
        .Select(s => new ShipmentResponse(s.Id, s.Reference, s.Status, s.CustomsValue))
        .SingleOrDefaultAsync(ct);
    return shipment is null ? TypedResults.NotFound() : TypedResults.Ok(shipment);
}
```

When the rule needs the object (owner, status), use a resource-based check.

```csharp
var result = await authz.AuthorizeAsync(user, shipment, ShipmentOperations.Update);
if (!result.Succeeded)
{
    return TypedResults.Forbid();
}

public sealed class ShipmentOwnerHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Shipment>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement, Shipment resource)
    {
        var userId = context.User.FindFirstValue("sub");
        if (requirement.Name == nameof(ShipmentOperations.Update)
            && userId is not null
            && resource.OwnerId == userId
            && resource.Status == "Draft")
        {
            context.Succeed(requirement);
        }

        // No Succeed call means deny. An exception here also means deny (fail closed).
        return Task.CompletedTask;
    }
}
```

- `[AllowAnonymous]` on a controller or a group opens each action in it. Put it on the exact action.
- .NET 10: cookie authentication returns 401 and 403 for API endpoints instead of a redirect to the sign-in page. Do not force redirects for APIs.

## Overposting and exposure

Bind to a request DTO with only the writable fields. Copy field by field. Return a response DTO.

```csharp
public sealed record UpdateShipmentRequest(
    [property: Required, StringLength(35, MinimumLength = 1)] string Reference,
    [property: Range(0, 10_000_000)] decimal CustomsValue);

shipment.Reference = request.Reference;       // copy field by field
shipment.CustomsValue = request.CustomsValue; // Status and TenantId stay server-side
```

Do not bind an entity, and do not call `Entry(entity).CurrentValues.SetValues(body)`.

## Cookies, BFF and antiforgery

For an Angular app, the BFF of SEC-03 keeps tokens on the server.

```csharp
builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(o =>
    {
        o.Cookie.Name = "__Host-app";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    })
    .AddOpenIdConnect(o =>
    {
        o.Authority = builder.Configuration["Auth:Authority"];
        o.ClientId = builder.Configuration["Auth:ClientId"];
        o.ClientSecret = builder.Configuration["Auth:ClientSecret"]; // from the secret store
        o.ResponseType = "code";                                     // code flow with PKCE (default on)
        o.SaveTokens = true;                                         // tokens stay in the server-side cookie ticket
        o.MapInboundClaims = false;
    });

// Angular HttpClient reads the XSRF-TOKEN cookie and sends the X-XSRF-TOKEN header.
builder.Services.AddAntiforgery(o => o.HeaderName = "X-XSRF-TOKEN");
```

The server writes the request token into a cookie that Angular can read:

```csharp
app.MapGet("/bff/xsrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
    {
        HttpOnly = false, // Angular must read it
        Secure = true,
        SameSite = SameSiteMode.Strict,
    });
    return Results.NoContent();
}).RequireAuthorization();
```

- Auth cookie defaults are `HttpOnly = true`, `SameSite = Lax`, `SecurePolicy = SameAsRequest`. Set `SecurePolicy = Always`.
- `UseAntiforgery` validates POST, PUT and PATCH only. Validate DELETE explicitly:

  ```csharp
  app.MapDelete("/drafts/{id:guid}", async (Guid id, IAntiforgery antiforgery, HttpContext context) =>
  {
      await antiforgery.ValidateRequestAsync(context);
      return Results.NoContent();
  }).RequireAuthorization();
  ```

- `HttpMethodOverrideMiddleware` in form-field mode before antiforgery turns a POST into a DELETE that skips the check.
- Minimal API endpoints that bind forms or `IFormFile` require antiforgery by default since .NET 8. Call `UseAntiforgery` after `UseAuthentication` and `UseAuthorization`.
- Pure bearer-token APIs do not need antiforgery. A browser does not send the token by itself.

## CORS

Default (SEC-09): no CORS, because SPA and API share one origin. When a public API needs CORS, list exact origins from configuration:

```csharp
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddPolicy("partners", p => p
    .WithOrigins(origins)
    .WithMethods("GET", "POST")
    .WithHeaders("Content-Type", "Authorization")));
```

`AllowAnyOrigin` with `AllowCredentials`, and `SetIsOriginAllowed(_ => true)`, let each web site call the API as the user. CORS is not authorization: it does not stop clients other than browsers.

## Data Protection

Antiforgery tokens, auth cookies and `IDataProtector` use the key ring. In a container, the default keys live in the container file system and are lost at restart. Share and protect the keys (SEC-20):

```csharp
builder.Services.AddDataProtection()
    .SetApplicationName("shipment-api")
    .PersistKeysToDbContext<AppDbContext>();
// Add ProtectKeysWithCertificate(cert) or ProtectKeysWithAzureKeyVault(...) for encryption at rest.
```

The `DbContext` implements `IDataProtectionKeyContext` with a `DbSet<DataProtectionKey> DataProtectionKeys`. Teams that skip this step often disable antiforgery later, because tokens fail after a deploy.

## Input validation and output encoding

- Validate at the boundary with DataAnnotations or FluentValidation: type, length, range, format, allowlist. Give each string a max length and each collection a max count.
- Validation lowers risk. It does not replace encoding or parameters.
- Razor encodes `@value`. `Html.Raw` and `MarkupString` do not.
- Encode for each sink: `HtmlEncoder.Default.Encode` for HTML outside Razor (mail, PDF templates), `UrlEncoder` for URL parts, parameters for SQL, message templates for logs.

## SQL with EF Core and Dapper

- LINQ first. EF Core parameterizes LINQ.
- SQL text: `FromSql($"...")`, `ExecuteSql($"...")`, `SqlQuery<T>($"...")`. The interpolated values become `DbParameter`s.
- `FromSqlRaw`, `ExecuteSqlRaw` and `SqlQueryRaw` only with constant SQL plus parameters. EF1002 (interpolation) and EF1003 (concatenation) warn in EF Core 10. The rules are in `dev-stack:efcore`.
- A dynamic sort or column comes from an allowlist:

  ```csharp
  query = sort switch
  {
      "value" => query.OrderBy(s => s.CustomsValue),
      "status" => query.OrderBy(s => s.Status),
      _ => query.OrderBy(s => s.Reference),
  };
  ```

- Dapper: pass a parameter object. `IN @Ids` expands to one parameter per value. `{=name}` literal replacement puts the value in the SQL text: use it only for constants.

  ```csharp
  connection.QueryAsync<ShipmentResponse>(
      "SELECT Id, Reference, Status, CustomsValue FROM dbo.Shipments WHERE TenantId = @TenantId AND Id IN @Ids",
      new { TenantId = tenantId, Ids = ids });
  ```

- `ExecuteSqlCommand` and `RawSqlString` are gone since EF Core 3.

## Deserialization

- System.Text.Json by default. For polymorphism, list the types:

  ```csharp
  [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
  [JsonDerivedType(typeof(ShipmentCreated), "shipment-created")]
  [JsonDerivedType(typeof(ShipmentCancelled), "shipment-cancelled")]
  public abstract record ShipmentEvent(Guid ShipmentId, Guid TenantId);
  ```

- Newtonsoft `TypeNameHandling` other than `None` lets the payload pick a type (CA2326 to CA2330). A compromised producer then has code execution in each consumer.
- BinaryFormatter throws `PlatformNotSupportedException` since .NET 9 (SYSLIB0011). The compatibility package `System.Runtime.Serialization.Formatters` brings it back with its risk. Do not add it.
- Apply the same rules to RabbitMQ messages and cache values.

## XML

Customs and partner messages are XML. `XmlReader.Create` defaults are safe: `DtdProcessing.Prohibit` and `XmlResolver = null`.

```csharp
public static XmlReader CreateXmlReader(Stream input) =>
    XmlReader.Create(input, new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit, // the default, written to make the intent clear
        XmlResolver = null,
        MaxCharactersInDocument = 10_000_000,
    });
```

Do not set `DtdProcessing.Parse` with an `XmlUrlResolver` on untrusted XML (CA3075). Do not run XSLT scripts from untrusted stylesheets (CA3076).

## SSRF

A URL that a user or a partner controls can point to an internal address, for example cloud metadata at `169.254.169.254`. Examples: a webhook, a logo URL, "import from URL", a partner redirect. Check the resolved address at connect time. A check on the URL string fails against DNS rebinding and redirects.

```csharp
private static readonly System.Net.IPNetwork[] Blocked =
[
    System.Net.IPNetwork.Parse("0.0.0.0/8"), System.Net.IPNetwork.Parse("10.0.0.0/8"),
    System.Net.IPNetwork.Parse("100.64.0.0/10"), System.Net.IPNetwork.Parse("127.0.0.0/8"),
    System.Net.IPNetwork.Parse("169.254.0.0/16"), System.Net.IPNetwork.Parse("172.16.0.0/12"),
    System.Net.IPNetwork.Parse("192.168.0.0/16"), System.Net.IPNetwork.Parse("::/128"),
    System.Net.IPNetwork.Parse("::1/128"), System.Net.IPNetwork.Parse("fc00::/7"),
    System.Net.IPNetwork.Parse("fe80::/10"),
];

public static bool IsBlocked(IPAddress address)
{
    if (address.IsIPv4MappedToIPv6)
    {
        address = address.MapToIPv4();
    }

    return Blocked.Any(n => n.Contains(address));
}

public static IHttpClientBuilder AddExternalHttpClient(this IServiceCollection services, string name) =>
    services.AddHttpClient(name)
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = async (context, ct) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
                if (addresses.Length == 0 || addresses.Any(IsBlocked))
                {
                    throw new HttpRequestException("The destination is not allowed.");
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            },
        });
```

- `AllowAutoRedirect` is `true` by default. A redirect to a private address skips a check that ran before the request.
- When the partners are known (carriers, customs gateways), add a host allowlist as well.
- Add container egress rules (NetworkPolicy) as a second layer.

## Paths and redirects

Generate file names on the server. When input must form a path, check the full path:

```csharp
public static string ResolveUnder(string baseDirectory, string relativePath)
{
    var root = Path.GetFullPath(baseDirectory);
    if (!root.EndsWith(Path.DirectorySeparatorChar))
    {
        root += Path.DirectorySeparatorChar;
    }

    var full = Path.GetFullPath(Path.Combine(root, relativePath));
    var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    if (!full.StartsWith(root, comparison))
    {
        throw new UnauthorizedAccessException("The path is outside the base folder.");
    }

    return full;
}
```

Redirect only to local URLs: `Results.LocalRedirect(url)` throws for a URL that is not local. In MVC, use `LocalRedirect` or check `Url.IsLocalUrl(returnUrl)`.

## Regex and ReDoS

The default regex timeout is infinite. Use a constant pattern with a timeout, and catch the timeout:

```csharp
[GeneratedRegex(@"^[A-Z]{2}\d{9}[A-Z]{2}$", RegexOptions.None, matchTimeoutMilliseconds: 100)]
private static partial Regex TrackingNumber();

public static bool IsTrackingNumber(string value)
{
    try
    {
        return value.Length <= 13 && TrackingNumber().IsMatch(value);
    }
    catch (RegexMatchTimeoutException)
    {
        return false; // fail closed
    }
}
```

Never build a pattern from user input. If you must, use `Regex.Escape`. `RegexOptions.NonBacktracking` and timeouts protect against slow input, not against a hostile pattern.

## Commands

Avoid processes. When one is necessary: a fixed executable path, no shell (`sh -c`, `cmd /c`), allowlisted arguments. OWASP warns that `ProcessStartInfo.ArgumentList` alone does not make untrusted input safe.

## Errors

- `AddProblemDetails` plus `UseExceptionHandler` outside Development. Never return `ex.Message` or `ex.ToString()`.
- The developer exception page is on by default only in Development. Keep an explicit call inside `IsDevelopment()`.
- An exception in an authorization path means deny (A10:2025).
- Roll back a partial write with a transaction when a later step fails.

## Logging

- Log failed sign-ins and denied access with a stable `EventId`, the user and the correlation id (SEC-23).
- Never log passwords, tokens, keys, connection strings, full request bodies or government ids.
- Use message templates or `[LoggerMessage]` (CA2254). Interpolated user text can forge log lines.
- Mark personal data with a data class and turn on redaction (SEC-14):

  ```csharp
  public sealed class PersonalDataAttribute() : DataClassificationAttribute(DataClasses.Personal);

  [LoggerMessage(EventId = 4001, Level = LogLevel.Warning, Message = "Sign-in failed for {UserName} from {ClientIp}")]
  public static partial void SignInFailed(ILogger logger, [PersonalData] string userName, string? clientIp);

  builder.Logging.EnableRedaction(); // package Microsoft.Extensions.Telemetry
  builder.Services.AddRedaction(r =>
  {
      r.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataClasses.Secret));
      r.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataClasses.Personal));
  });
  ```

  Use an HMAC redactor instead of `ErasingRedactor` when support must correlate log lines of one person. Serilog as the logger factory stops redaction (`dev-stack:dotnet-observability`).

## Crypto and random values

```csharp
public static string NewResetToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

public static string NewOneTimeCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

// Store only a hash of an API key or reset token. Compare in constant time.
public static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

public static bool TokenMatches(string presented, byte[] storedHash) =>
    CryptographicOperations.FixedTimeEquals(HashToken(presented), storedHash);

// Integrity of a webhook body: HMAC with a key from the secret store.
public static bool SignatureMatches(byte[] key, byte[] body, byte[] signature) =>
    CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, body), signature);
```

- SHA256 is correct for a random token with 256 bits of entropy. It is wrong for a password: use the identity provider or `PasswordHasher<T>`.
- `PasswordHasher<T>` defaults to PBKDF2 with 100,000 iterations. OWASP asks 220,000 for PBKDF2-HMAC-SHA512. Raise `PasswordHasherOptions.IterationCount` after a load test. Do not call `KeyDerivation.Pbkdf2` directly for stored passwords.
- `AesGcm` with a unique nonce per encryption. Data Protection for short-lived protected payloads.
- `Guid.NewGuid()` is not a secret.

## Rate limits and size limits

```csharp
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("expensive", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.WebHost.ConfigureKestrel(k =>
{
    k.Limits.MaxRequestBodySize = 1_000_000;
    k.AddServerHeader = false;
});
```

- The default rejection status is 503. Clients retry 503 as an outage. Set 429.
- Partition by user id, with the client IP as fallback. The client IP is only correct after forwarded headers.
- Cap page size (`Math.Clamp(pageSize ?? 50, 1, 100)`). No unbounded `ToListAsync()` on a user filter.

## Headers, HTTPS and proxies

ASP.NET Core has no built-in middleware for CSP and related headers. A small middleware is enough for an API (SEC-10):

```csharp
public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
    app.Use(async (context, next) =>
    {
        var headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        await next();
    });
```

- The CSP of a JSON API is strict. The Angular app needs its own CSP (`frontend.md`).
- `UseHsts` defaults to 30 days without subdomains. Start short, then one year (SEC-08). APIs can reject plain HTTP instead of a redirect.
- Behind a proxy, trust only the gateway:

  ```csharp
  builder.Services.Configure<ForwardedHeadersOptions>(o =>
  {
      o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
      o.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
  });
  ```

  `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` clears the known lists and trusts any proxy. A client then fakes its IP, which defeats rate limits and audit logs.
- Map Swagger UI and the OpenAPI document only in Development, or protect them (API9).

## Version notes

- .NET 10: `KnownNetworks` and `Microsoft.AspNetCore.HttpOverrides.IPNetwork` are obsolete (ASPDEPR005). Use `KnownIPNetworks` with `System.Net.IPNetwork`. On .NET 8 and 9, use `KnownNetworks`.
- .NET 10: cookie authentication returns 401 and 403 for API endpoints.
- .NET 10 SDK: NuGetAudit checks transitive packages by default. `dotnet package list` and `dotnet package update --vulnerable` are the new command forms.
- .NET 10 servicing: use runtime and ASP.NET Core 10.0.14 or later, a security release of 2026-10-25. 10.0.13 (2026-10-13) fixed 8 ASP.NET Core advisories, two of them critical (security feature bypass, CVE-2026-88092 and CVE-2026-70188). 8.0.32 and 9.0.21 fix the same advisories. Update the shared framework and the base image digest.
- EF Core 10: analyzer EF1003 for concatenation in raw SQL is new. EF1002 for interpolation in raw SQL existed before.
- .NET 9: BinaryFormatter throws at run time.
- .NET 8 and 9 end support on 2026-11-10. Tell the human.
