---
name: secure-coding
description: Security rules for writing and reviewing code in the .NET 10, ASP.NET Core, EF Core, Angular and AngularJS stack, with its integrations, supply chain, containers and Kubernetes config. Covers authentication, authorization and tenant isolation, injection, deserialization, SSRF, file upload, secrets, crypto, CSRF, CORS, headers, logging, dependencies and LLM calls, mapped to OWASP Top 10 2025, API Top 10 2023, ASVS 5.0 level 2 and CWE Top 25 2025. Use when code adds or changes an endpoint, sign-in, a permission check, input binding, SQL, a file or path, an outbound URL, a serializer, a secret or key, a cookie or token, a log of user data, a dependency, a Dockerfile, a manifest, a CI workflow or an LLM call, or when the user asks for a security review, a threat model, or "is this safe". Not for general code review without a security question, pure styling or text changes, or token issuing inside the identity provider.
metadata:
  verified: "2026-10-16"
  versions: ".NET 10 (SDK 10.0.402, ASP.NET Core 10.0.13), EF Core 10, Angular 22, AngularJS 1.8, FileSignatures 7.3.0, MailKit 4.18.1, PuppeteerSharp 25.14.0, Trivy 0.74.0, OWASP Top 10:2025, API Security Top 10:2023, ASVS 5.0.0, CWE Top 25:2025, OWASP LLM Top 10:2026"
---

# Secure coding

Security rules for each change in this stack, for the writer and for the reviewer. Framework use is in the stack skills (`dev-stack:aspnetcore`, `dev-stack:efcore`, `dev-stack:angular`). This skill adds what makes that use safe. EF Core raw SQL rules are in `dev-stack:efcore` (rules `EF-RAW-SQL-*`).

## First

1. Read `references/stack-rules.md`.
2. Read the versions: `TargetFramework`, `Directory.Packages.props`, `package.json`, image tags. The rules are for `net10.0` and Angular 22. For `net8.0` or `net9.0`, also read the section "Version notes" in `references/backend-dotnet.md`.
3. Name each trust boundary that the change touches: browser to API, API to a store, API to an external service. Stores are SQL, cache, search, queue and files. External services are SMTP, headless browser, partner HTTP and LLM. Done when you can name each boundary and the control for it.
4. Read the reference for each boundary (section "References"). Read only the files that the change needs.

## Defaults for new code

Existing code keeps its style. Ask each open decision when the work first needs it (stack rule 4). Detail and options are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Identity provider | Keycloak for customer products, Entra ID for staff apps | SEC-01 |
| API tokens | JWT bearer, validated locally, one audience per API | SEC-02 |
| Angular sign-in | BFF: OIDC on the server, `HttpOnly` cookie, antiforgery | SEC-03 |
| Authorization | Fallback policy plus named permission policies, resource checks | SEC-04 |
| Tenant isolation | `TenantId` from the token, EF query filter, cross-tenant test | SEC-05 |
| Passwords | The identity provider owns them | SEC-06 |
| Secrets | User-secrets in development, platform store and OIDC in CI | SEC-07 |
| TLS | Ends at the gateway, HSTS one year, known proxies | SEC-08 |
| CORS | None: one origin through the BFF | SEC-09 |
| Headers | OWASP set: nginx for static files, middleware for APIs | SEC-10 |
| Rate limits | Built-in limiter, 429, per user, on costly endpoints | SEC-11 |
| Size limits | 1 MB body, raised per endpoint, page size 100 | SEC-12 |
| Uploads | Allowlist, FileSignatures, private storage, malware scan | SEC-13 |
| Logs | Data classes plus redaction, no secrets | SEC-14 |
| Dependencies | Lock files, source mapping, audit, 3-day cooldown | SEC-15 |
| SAST | `AnalysisModeSecurity=All` plus Semgrep CE, CodeQL when licensed | SEC-16 |
| Secret scan | gitleaks in pre-commit and CI | SEC-17 |
| Threat model | Five STRIDE questions per design with a new boundary | SEC-18 |
| Review gate | Block Critical and High, ASVS 5.0 level 2 | SEC-19 |
| Data Protection keys | Database, protected with a certificate or vault key | SEC-20 |
| Kubernetes | Pod Security `restricted`, default-deny NetworkPolicy | SEC-21 |
| Service to service | Client credentials JWT, one RabbitMQ user per service | SEC-22 |
| Audit | Security events with stable `EventId`, audit table | SEC-23 |

## Rules

### Mental model

- Untrusted data crosses a trust boundary and reaches a sink. Each crossing needs a named control.
- Three control families: identity (authentication), permission (function, object and field), data handling (validate at entry, encode or parameterize at the sink).
- Validation is not encoding. Validate business rules at the boundary. Encode for each sink: HTML, SQL, shell, URL, log.
- The framework is safe by default. Most real bugs are opt-outs: `Html.Raw`, `bypassSecurityTrust*`, `FromSqlRaw`, `TypeNameHandling`, `ValidateLifetime = false`, `DisableAntiforgery`, `--no-sandbox`.
- Fail closed. When a check throws or is not sure, deny.

### Access control (A01, API1, API3, API5, CWE-862, CWE-639)

1. Set a fallback policy that requires an authenticated user. A new endpoint is then closed by default.
2. Protect each endpoint with a named permission policy: `RequireAuthorization("shipments:write")`. Do not check role strings in handlers.
3. Read the tenant and the user from validated claims only. A tenant id from the route, query or body is an IDOR.
4. Filter each query by tenant, with an EF Core global query filter on `TenantId`. Return 404 for objects of another tenant.
5. Use `IAuthorizationService.AuthorizeAsync(user, resource, requirement)` when the rule needs the object (owner, status).
6. Bind requests to a `sealed record` DTO with only writable fields. Return a response DTO, never an entity.
7. Put `[AllowAnonymous]` only on the exact public action. List each anonymous route in an authorization test.

### Authentication, sessions, CSRF (A07, CWE-352)

8. Let `AddJwtBearer` validate issuer, audience, lifetime and signature. Set `Authority`, `Audience`, `ValidAlgorithms` and `MapInboundClaims = false`.
9. Keep tokens out of the browser. Use a BFF with a `__Host-` cookie: `HttpOnly`, `Secure`, `SameSite=Strict` or `Lax`.
10. When the app signs users in with a cookie, keep antiforgery on for each state change. `UseAntiforgery` checks POST, PUT and PATCH only, so call `IAntiforgery.ValidateRequestAsync` for DELETE. When the endpoint accepts only a bearer token, an API key, or no sign-in, it is not open to CSRF: a minimal API endpoint that binds `IFormFile` or `[FromForm]` then needs `.DisableAntiforgery()` with the reason on the line, because since .NET 8 such an endpoint requires a token by default.
10a. When the sign-in decision is open (SEC-01, SEC-02, SEC-04) and nobody answers, keep the sign-in setup that the app has, build the feature, and put the missing sign-in as a blocking open point. Do not add a placeholder authentication scheme that rejects all calls.
11. Rate limit sign-in, reset, OTP, export, render, upload and LLM endpoints. Set `RejectionStatusCode = 429`.

### Injection and parsing (A05, A08, CWE-89, CWE-79, CWE-502)

12. Use LINQ first. For SQL text use `FromSql($"...")` or Dapper with a parameter object. Map dynamic columns through an allowlist.
13. Deserialize with System.Text.Json. For polymorphism, use `[JsonPolymorphic]` with `[JsonDerivedType]`. Treat queue and cache payloads as untrusted.
14. Keep the safe `XmlReader` defaults: `DtdProcessing.Prohibit`, `XmlResolver = null`. Treat partner XML as untrusted.
15. Build regexes from constants, with `[GeneratedRegex(..., matchTimeoutMilliseconds: 100)]`. Catch `RegexMatchTimeoutException` and deny.
16. Log with message templates or `[LoggerMessage]`, never with `$""`. Interpolation lets user text forge log lines.

### Outbound calls, files, redirects (A01 SSRF, CWE-918, CWE-22, CWE-434)

17. For a URL that a user or a partner controls, use a host allowlist and a connect-time IP check. Set `AllowAutoRedirect = false`.
18. Generate file names on the server. When input forms a path, resolve with `Path.GetFullPath` and check the base folder prefix.
19. For uploads, follow `references/file-upload.md`: size, extension allowlist, FileSignatures, quarantine, attachment download.
20. Redirect only to local URLs: `Results.LocalRedirect` or `Url.IsLocalUrl`.

### Secrets, crypto, errors, logs (A02, A04, A09, A10)

21. Keep secrets out of source, `appsettings*.json`, image layers, `ARG`/`ENV` and logs. Read them from the secret store.
22. Use `RandomNumberGenerator` for tokens and codes. Compare secrets with `CryptographicOperations.FixedTimeEquals`. Store only a hash of a token.
23. Use Data Protection, `AesGcm` and `HMACSHA256`. Do not write your own crypto, password hash or JWT parser.
24. Return ProblemDetails with a generic message. Keep `UseDeveloperExceptionPage` in an `IsDevelopment()` check. Log the detail on the server.
25. Log failed sign-ins and denied access with a stable `EventId`. Mark personal data with a data class and turn on redaction.

### Supply chain and platform (A03, A02)

26. Before you add a package, check that it exists, its age, its owner and its license. Then ask the human (stack rule 5).
27. Commit lock files. Restore with `--locked-mode` and `npm ci`. Keep NuGetAudit on. Pin CI actions and scanners by SHA or digest.
28. Run containers as non-root (`USER $APP_UID`), and meet Pod Security `restricted`. Detail: `references/containers-kubernetes.md`.

## Never write

| Wrong (what an LLM writes) | Correct |
|----------------------------|---------|
| Endpoint with no policy because the prompt did not ask | Fallback policy plus `RequireAuthorization("<permission>")` |
| `db.Orders.FindAsync(id)` | Query filter on `TenantId`, or `Where(o => o.Id == id && o.TenantId == tenantId)` |
| `customerId` from the query string | Tenant and user from `User` claims |
| Bind the request body to the entity | Request DTO, copy field by field |
| `conn.QueryAsync($"... '{x}'")` | `conn.QueryAsync("... @X", new { X = x })` |
| `TypeNameHandling.Auto` to "fix polymorphism" | `[JsonPolymorphic]` and `[JsonDerivedType]` |
| `BinaryFormatter` or its compatibility package | System.Text.Json with a typed contract |
| `(_, _, _, _) => true` certificate callback, `TrustServerCertificate=True` | Fix the CA chain in the container |
| `AllowAnyOrigin()` with `AllowCredentials()`, `SetIsOriginAllowed(_ => true)` | One origin, or `WithOrigins(config)` |
| `ValidateIssuer = false`, `ValidateAudience = false`, `ValidateLifetime = false` | Correct `Authority` and `Audience` |
| `ReadJwtToken` as a check | JwtBearer, or `JsonWebTokenHandler.ValidateTokenAsync` |
| `SHA256(password + salt)` | The identity provider, or `PasswordHasher<T>` |
| `new Random()` for a code or token | `RandomNumberGenerator.GetInt32` or `GetBytes` |
| `catch (Exception ex) { return BadRequest(ex.Message); }` | ProblemDetails plus a server-side log |
| `logger.LogInformation($"User {name}")` | `logger.LogInformation("User {UserName}", name)` |
| `.DisableAntiforgery()` on a cookie-authenticated endpoint because a form test fails | Send the token in the test |
| Antiforgery on a form endpoint of an API without cookies | `.DisableAntiforgery()` with the reason: no cookie, so no CSRF |
| `bypassSecurityTrustHtml(userHtml)` to stop stripping | Bind with `[innerHTML]` and let Angular sanitize |
| `localStorage.setItem('token', ...)` | BFF cookie, or a token in memory |
| `--no-sandbox` "for Docker" | Non-root user with the sandbox on |
| `Path.Combine(root, file.FileName)` | `Path.Combine(root, $"{Guid.NewGuid():N}{ext}")` |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`, `KnownNetworks` | `KnownProxies` or `KnownIPNetworks` |
| `uses: actions/checkout@v4` | `uses: actions/checkout@<40-hex-sha> # v4.x.y` |
| A package name from memory | Look it up on nuget.org or npmjs.com first |
| Own crypto wrapper, own sanitizer, own JWT code | The framework feature |

## Check

Run these after each change. Fix each error. Fix each warning or give the reason to keep it.

1. Rule check of this skill. A pass is `0 errors`:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo> --changed <base branch>
   ```

2. Build with the security analyzers on. Most CA security rules are off by default. A pass is 0 warnings in the listed IDs:

   ```
   dotnet build -p:AnalysisModeSecurity=All -warnaserror:CA2100,CA2300,CA2301,CA2326,CA2327,CA3075,CA5350,CA5351,CA5359,CA5394,EF1002,EF1003
   ```

3. Dependencies. A pass is "no vulnerable packages":

   ```
   dotnet package list --vulnerable --include-transitive
   npm audit --omit=dev
   ```

4. Secrets. A pass is exit code 0: `gitleaks git --redact` (or `gitleaks dir --redact` without Git history).
5. The authorization test and the cross-tenant test of `references/testing.md` pass.

The reviewer also runs Semgrep CE or Opengrep, Trivy, and CodeQL when licensed. Commands and limits are in `references/tools.md`.

## Review checklist

The full list, with the ASVS and CWE mapping, is in `references/review-checklist.md`.

1. Each new endpoint has a policy, or is in the anonymous list of the test. `tool (SC-ALLOW-ANONYMOUS)` + `judge`
2. Each query by id filters by tenant or owner from claims, or calls a resource check. `judge`
3. Requests bind to DTOs, responses return DTOs. `judge`
4. No SQL built from strings in EF raw methods, Dapper or `SqlCommand`. `tool (EF1002, EF1003, CA2100, SC-DAPPER-SQL-INTERP)`
5. No unsafe deserializer. `tool (SC-TYPENAMEHANDLING, SC-BINARYFORMATTER, CA2326, CA2300)`
6. Outbound calls to user-influenced URLs have an allowlist and an IP check. `judge`
7. No client file name in a path. Uploads follow the pipeline. `tool (SC-CLIENT-FILENAME-PATH)` + `judge`
8. JWT validation is complete. `tool (SC-JWT-*)`
9. CORS lists exact origins. `tool (SC-CORS-*)`
10. Cookie-authenticated state changes check antiforgery, DELETE too. `tool (SC-DISABLE-ANTIFORGERY)` + `judge`
11. No secrets in code, config, images or CI. `tool (SC-APPSETTINGS-SECRET, SC-PRIVATE-KEY, gitleaks)`
12. No weak crypto, no disabled certificate check, `RandomNumberGenerator` for secrets. `tool (SC-CERT-ACCEPT-ALL, SC-RANDOM-SECRET, CA5351, CA5359, CA5394)`
13. Errors give no internals. Authorization code fails closed. `tool (SC-DEV-EXCEPTION-PAGE)` + `judge`
14. Angular: no unsafe DOM writes, no token storage. `tool (SC-TS-*)`
15. New packages exist, are mature and come from mapped sources. CI actions have a SHA pin. `tool (SC-ACTION-NOT-PINNED, NuGetAudit)` + `judge`

The security reviewer reports with the format in `references/review-report.md`.

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). A security review uses the same headings with the verdict words and the findings columns of `references/review-report.md`.

## References

Read the file for each boundary that the change touches.

- `references/decisions.md`: the SEC decisions with options. Read when new code needs a decision that has no accepted answer.
- `references/backend-dotnet.md`: authentication, authorization, JWT, cookies, antiforgery, CORS, Data Protection, input, SQL, deserialization, XML, SSRF, paths, redirects, regex, errors, logs, crypto, rate limits, headers, proxies. Read for each API change.
- `references/file-upload.md`: the upload pipeline with FileSignatures. Read when code accepts or serves user files.
- `references/integrations.md`: Valkey and Redis, RabbitMQ, Elasticsearch, MailKit, PuppeteerSharp. Read when the change uses one of them.
- `references/frontend.md`: Angular sanitizer, `bypassSecurityTrust`, CSP, Trusted Types, XSRF, token storage, AngularJS. Read for each frontend change.
- `references/supply-chain.md`: NuGet, npm, lock files, source mapping, cooldown, package check, CI pinning, Trivy incident. Read when a dependency or a workflow changes.
- `references/containers-kubernetes.md`: Dockerfile and manifest security. Read when a Dockerfile or a manifest changes.
- `references/llm-apps.md`: OWASP LLM Top 10:2026. Read only when the code calls an LLM.
- `references/threat-model.md`: the five STRIDE questions. Read when a design adds a trust boundary.
- `references/testing.md`: security tests (authorization inventory, BOLA, JWT, SSRF, paths). Read when you write tests for a security control.
- `references/tools.md`: local tools, flags, licenses and blind spots. Read before you run a scanner.
- `references/reference-lists.md`: OWASP, API, ASVS, CWE and LLM lists with the code mistake of this stack. Read to map a finding to an ID.
- `references/review-checklist.md`: the full checklist. The security reviewer reads it for each review.
- `references/review-report.md`: the report format for the human. The security reviewer reads it before it writes.
- `references/sources.md`: primary sources with the date checked.
