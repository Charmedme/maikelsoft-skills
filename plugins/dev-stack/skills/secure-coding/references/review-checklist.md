# Security review checklist

The full checklist for the security reviewer and for the writer as a self-check. Each check is yes or no. `tool` means a rule or a tool decides it. `judge` means it needs reasoning. The target is ASVS 5.0 level 2 (SEC-19). Write each finding in the format of `review-report.md`.

## Contents

- How to use
- Access control
- Authentication and sessions
- Input, injection and parsing
- Outbound calls, files and redirects
- Secrets, crypto and data protection
- Errors, logging and resource limits
- Integrations
- Frontend
- Supply chain and CI
- Containers and Kubernetes
- LLM calls
- Design
- Analyzer IDs

## How to use

1. Run the tools first (SKILL.md section "Check"). A tool finding is a candidate, not yet a finding.
2. Select the sections for the boundaries that the diff touches. Always do "Access control".
3. For each `judge` check, trace the input from the source to the sink. Sources are route, query, body, header, message, file, partner response and model output.
4. Verify each candidate before you report it (`review-report.md`, section "Process before the report").

## Access control

| # | Check | Type | Maps to |
|---|-------|------|---------|
| AC-1 | A fallback policy that requires an authenticated user exists. | judge (test in `testing.md`) | A01, CWE-862, ASVS V8 |
| AC-2 | Each new or changed endpoint has a named policy, or is in the anonymous list of the authorization test. | tool (SC-ALLOW-ANONYMOUS) + judge | A01, API5, CWE-862 |
| AC-3 | `[AllowAnonymous]` is only on the exact public action, not on a controller or a group. | tool (SC-ALLOW-ANONYMOUS) + judge | A01, CWE-306 |
| AC-4 | Each query by id filters by tenant or owner from validated claims, or calls `IAuthorizationService.AuthorizeAsync` with the resource. | judge | API1, CWE-639 |
| AC-5 | No tenant id, user id or role comes from the route, query, body or a header that the client sets. | judge | API1, CWE-639 |
| AC-6 | Dapper queries, raw SQL, `IgnoreQueryFilters()` and bulk operations add the tenant condition themselves. The EF query filter does not cover them. | judge | API1, CWE-639 |
| AC-7 | An object of another tenant gives 404, not 403, so the API does not confirm that it exists. | judge | API1, CWE-200 |
| AC-8 | Requests bind to DTOs with only writable fields. No `SetValues(body)`, no entity as a parameter. | judge | API3, CWE-915 |
| AC-9 | Responses return DTOs, never entities. No `PasswordHash`, internal flags or other tenants in the output. | judge | API3, CWE-200 |
| AC-10 | Admin functions have their own policy. A hidden menu in the UI is not a control. | judge | API5, CWE-285 |
| AC-11 | State that the server owns (status, price, approval, owner) is not taken from the request. | judge | A06, CWE-602 |
| AC-12 | Authorization code fails closed: an exception, a null or an unknown value means deny. No `catch { return true; }`. | judge | A10, CWE-636 |

## Authentication and sessions

| # | Check | Type | Maps to |
|---|-------|------|---------|
| AU-1 | JWT: issuer, audience, lifetime and signature are validated. No `Validate* = false`, no `RequireSignedTokens = false`. | tool (SC-JWT-NO-LIFETIME, SC-JWT-NO-AUDIENCE, SC-JWT-NO-ISSUER, SC-JWT-UNSIGNED) | A07, API2, ASVS V9 |
| AU-2 | `ValidAlgorithms` is pinned. `ClockSkew` is 30 seconds to 2 minutes. | judge | ASVS V9 |
| AU-3 | `RequireHttpsMetadata = false` only behind an environment check for local development. | tool (SC-JWT-HTTP-METADATA) | A02, CWE-319 |
| AU-4 | No `ReadJwtToken` or `ReadToken` result used for a security decision. | tool (SC-JWT-READ-NOT-VALIDATE) | API2, CWE-345 |
| AU-5 | No signing key or client secret in source code. | tool (SC-JWT-HARDCODED-KEY, SC-APPSETTINGS-SECRET) | CWE-798 |
| AU-6 | Cookies: `HttpOnly`, `SecurePolicy = Always`, `SameSite` Lax or Strict, `__Host-` prefix for the session cookie. | judge | ASVS V3, V7 |
| AU-7 | Cookie-authenticated POST, PUT and PATCH have antiforgery. DELETE calls `IAntiforgery.ValidateRequestAsync`. | tool (SC-DISABLE-ANTIFORGERY) + judge | A01, CWE-352 |
| AU-8 | `HttpMethodOverrideMiddleware` in form mode does not run before antiforgery. | judge | CWE-352 |
| AU-9 | Sign-in, reset, OTP and verification mail have a rate limit and do not reveal if an account exists. | judge | A07, API6, CWE-307 |
| AU-10 | Reset and verification tokens are random (`RandomNumberGenerator`), single use, expire, and the server stores only a hash. | tool (SC-RANDOM-SECRET) + judge | A07, CWE-640 |
| AU-11 | API keys: the server stores a hash and compares with `CryptographicOperations.FixedTimeEquals`. | judge | API2, CWE-208 |
| AU-12 | Data Protection keys are shared between replicas and protected at rest (SEC-20). | judge | A02 |

## Input, injection and parsing

| # | Check | Type | Maps to |
|---|-------|------|---------|
| IN-1 | No SQL built from strings in `FromSqlRaw`, `ExecuteSqlRaw`, `SqlQueryRaw`, Dapper or `SqlCommand.CommandText`. | tool (EF1002, EF1003, CA2100, SC-DAPPER-SQL-INTERP, EF-RAW-SQL-*) | A05, CWE-89 |
| IN-2 | A dynamic column, sort or table name comes from an allowlist map. | judge | A05, CWE-89 |
| IN-3 | Dapper `{=name}` literal replacement only for constants. | judge | CWE-89 |
| IN-4 | Each string field has a max length. Numbers have a range. Collections have a max count. | judge | A05, CWE-20, CWE-770 |
| IN-5 | No `TypeNameHandling` other than `None` without a strict binder. No BinaryFormatter, no compatibility package. | tool (SC-TYPENAMEHANDLING, SC-BINARYFORMATTER, SC-PKG-BINARYFORMATTER, CA2326, CA2327, CA2300, CA2301) | A08, CWE-502 |
| IN-6 | Queue and cache payloads are validated like a request body, and the tenant in a message is checked again. | judge | A08, API10 |
| IN-7 | XML readers keep `DtdProcessing.Prohibit` and `XmlResolver = null`. No XSLT scripts from untrusted stylesheets. | tool (CA3075, CA3076) + judge | A05, CWE-611 |
| IN-8 | No regex pattern from user input. Regexes on user input have a timeout or `NonBacktracking`, and the code catches the timeout. | tool (CA3012) + judge | CWE-1333 |
| IN-9 | No `Process.Start` with a shell or with user input in the arguments. | tool (SC-PROCESS-SHELL) + judge | A05, CWE-78 |
| IN-10 | No Roslyn scripting, `DynamicExpressionParser` or `Expression` compile on user input. | judge | CWE-94 |
| IN-11 | Razor: no `Html.Raw` or `MarkupString` with user data. | tool (SC-HTML-RAW) | A05, CWE-79 |
| IN-12 | Elasticsearch: no raw user input in `query_string`. The tenant filter is in each query. | judge | A05, API1 |
| IN-13 | Data from partner APIs is validated before it reaches SQL, HTML, a path or a URL. | judge | API10 |

## Outbound calls, files and redirects

| # | Check | Type | Maps to |
|---|-------|------|---------|
| OF-1 | Outbound HTTP to a URL that a user or partner influences has a host allowlist or a connect-time IP check, and `AllowAutoRedirect = false`. | judge (entry points: `GetAsync(`, `SendAsync(`, `new Uri(`, webhook URLs) | A01, API7, CWE-918 |
| OF-2 | Headless browser navigation and rendering block network requests other than `data:` and allowlisted hosts. | judge | A01, CWE-918 |
| OF-3 | No client file name in a path. Storage names come from the server. | tool (SC-CLIENT-FILENAME-PATH) + judge | CWE-22, CWE-434 |
| OF-4 | When input forms a path, the code resolves the full path and checks the base folder prefix. | judge | CWE-22 |
| OF-5 | Uploads follow the pipeline of `file-upload.md` (UP-1 to UP-12). | judge | CWE-434, ASVS V5 |
| OF-6 | Redirects go to local URLs only (`LocalRedirect`, `IsLocalUrl`) or to an allowlist. | judge | CWE-601 |

## Secrets, crypto and data protection

| # | Check | Type | Maps to |
|---|-------|------|---------|
| SC-1 | No secrets, keys or real connection strings in code, `appsettings*.json`, tests, Dockerfiles or CI files. | tool (SC-APPSETTINGS-SECRET, SC-PRIVATE-KEY, SC-DOCKER-SECRET-ARG, gitleaks) | CWE-798, ASVS V13 |
| SC-2 | No MD5, SHA1, DES, TripleDES, RC2 or ECB. | tool (CA5350, CA5351, CA5358, SC-WEAK-HASH-SECRET) | A04, CWE-327 |
| SC-3 | No hard-coded key or IV. AES uses `AesGcm` with a unique nonce per encryption. | tool (CA5390) + judge | A04, CWE-321 |
| SC-4 | No certificate callback that returns true. No `TrustServerCertificate=True` outside local development. | tool (SC-CERT-ACCEPT-ALL, SC-TRUST-SERVER-CERT, CA5359) | A04, CWE-295 |
| SC-5 | Passwords: the identity provider, or `PasswordHasher<T>` with raised iterations. No own hash. | tool (SC-WEAK-HASH-SECRET) + judge | A04, CWE-916 |
| SC-6 | Tokens, codes and nonces come from `RandomNumberGenerator`. | tool (SC-RANDOM-SECRET, CA5394) | CWE-330 |
| SC-7 | Personal data in cache, search index, e-mail, PDF and LLM prompt is needed and limited. | judge | ASVS V14 |

## Errors, logging and resource limits

| # | Check | Type | Maps to |
|---|-------|------|---------|
| EL-1 | Errors return ProblemDetails with a generic message. No `ex.Message`, `ex.ToString()` or stack trace in a response. | tool (ASP-EXCEPTION-MESSAGE) + judge | A10, CWE-209 |
| EL-2 | `UseDeveloperExceptionPage` only in an `IsDevelopment()` check. | tool (SC-DEV-EXCEPTION-PAGE) | A02, CWE-209 |
| EL-3 | A partial write rolls back (transaction) when a later step fails. | judge | A10 |
| EL-4 | Logs have no passwords, tokens, keys, connection strings, full bodies or government ids (BSN). | tool (SC-LOG-SECRET, EF-SENSITIVE-LOGGING) + judge | A09, CWE-532 |
| EL-5 | Log calls use message templates or `[LoggerMessage]`, not interpolation. | tool (CA2254) | A09, CWE-117 |
| EL-6 | Failed sign-ins, denied access and admin actions are logged with user, correlation id and a stable `EventId`. | judge | A09, ASVS V16 |
| EL-7 | Costly endpoints have a rate limit with status 429 (sign-in, export, render, upload, LLM). | judge | API4, API6, CWE-770 |
| EL-8 | Request size has a limit per endpoint. No `DisableRequestSizeLimit`. Page size has a max. No unbounded `ToListAsync()` on a user filter. | tool (SC-NO-SIZE-LIMIT) + judge | API4, CWE-770 |
| EL-9 | Forwarded headers trust only known proxies. | tool (SC-FORWARDED-TRUST-ALL) | A02 |
| EL-10 | Security headers are present on HTML and API responses (SEC-10). Swagger UI is not public in production. | judge | A02, API9 |

## Integrations

| # | Check | Type | Maps to |
|---|-------|------|---------|
| IG-1 | Valkey or Redis: an ACL user with a password and least rights, TLS, the tenant in each key, a safe serializer for values. | judge | A02, API1 |
| IG-2 | RabbitMQ: no `guest` user, one user and vhost per service, TLS, each message validated, the tenant checked again. | judge | A07, API1, A08 |
| IG-3 | Elasticsearch: an API key with index rights only, the tenant filter in each query and in vector search, no `query_string` with raw input. | judge | API1, A05 |
| IG-4 | MailKit: `StartTls` or `SslOnConnect`, no accept-all certificate callback, CR and LF rejected in addresses, user values HTML-encoded, `From` is a domain that you control. MimeKit is 4.15.1 or later. | tool (SC-SMTP-NO-TLS, SC-CERT-ACCEPT-ALL) + judge | A04, CWE-93 |
| IG-5 | PuppeteerSharp with untrusted HTML: JavaScript off, request interception that allows only `data:` and `about:blank`, no `--no-sandbox`, a separate low-rights worker at scale. | tool (SC-NO-SANDBOX) + judge | A01, CWE-918 |

## Frontend

| # | Check | Type | Maps to |
|---|-------|------|---------|
| FE-1 | No new `bypassSecurityTrust*` on data that the code did not build from validated parts. | tool (SC-TS-BYPASS-TRUST) + judge | A05, CWE-79 |
| FE-2 | No `innerHTML` or `outerHTML` writes, `insertAdjacentHTML` or `document.write`. | tool (SC-TS-INNERHTML, SC-TS-DOM-WRITE) | A05, CWE-79 |
| FE-3 | No tokens or session ids in `localStorage` or `sessionStorage`. | tool (SC-TS-TOKEN-STORAGE) | A07, ASVS V14 |
| FE-4 | The XSRF cookie and header pair works with the server (`X-XSRF-TOKEN`). | judge | CWE-352 |
| FE-5 | A CSP exists for the Angular app (nonce or `autoCsp`). Trusted Types only list the policy names that the app needs. | judge | A02, ASVS V3 |
| FE-6 | No Angular template built from user input. Production builds use AOT. | judge | CWE-94 |
| FE-7 | AngularJS: no user data in server-rendered templates (or `ng-non-bindable` around it), no `$sce.trustAs*` or `$eval`, `$compile`, `$parse` on user input. | tool (SC-NG1-TRUST-HTML) + judge | CWE-79, CWE-94 |

## Supply chain and CI

| # | Check | Type | Maps to |
|---|-------|------|---------|
| SU-1 | No new vulnerable package, direct or transitive. | tool (NuGetAudit NU1901 to NU1904, `dotnet package list --vulnerable --include-transitive`, `npm audit`) | A03 |
| SU-2 | Each new package exists on the registry with that exact name, has an owner and a history, a known license, and the human approved it. | judge | A03 |
| SU-3 | Lock files changed together with the dependency. CI uses `--locked-mode` and `npm ci`. | tool (SC-NPM-INSTALL-CI) + judge | A03 |
| SU-4 | NuGetAudit is on. Suppressions are per advisory, with a reason. | tool (SC-NUGETAUDIT-OFF) | A03 |
| SU-5 | A new private feed has Package Source Mapping. | judge | A03 |
| SU-6 | CI actions are pinned by commit SHA. `permissions:` is least privilege. Fork pull requests get no secrets. | tool (SC-ACTION-NOT-PINNED) + judge | A03 |
| SU-7 | Scanners are pinned by version and checksum or digest. No Trivy 0.69.4. | tool (SC-TRIVY-COMPROMISED) | A03 |
| SU-8 | No unmaintained security package (SecurityCodeScan). | tool (SC-PKG-SECURITYCODESCAN, SC-PKG-SECURITY-SCAN) | A03 |

## Containers and Kubernetes

| # | Check | Type | Maps to |
|---|-------|------|---------|
| CK-1 | The last `USER` is numeric non-root (`$APP_UID`), or the image is chiseled or distroless. | tool (hadolint DL3002) + judge | A02 |
| CK-2 | Base images are pinned to a patch tag or digest. Multi-stage build. | judge | A03 |
| CK-3 | No secrets in `ARG` or `ENV`. Build secrets use a secret mount. | tool (SC-DOCKER-SECRET-ARG, Docker build check SecretsUsedInArgOrEnv) | CWE-798 |
| CK-4 | Pod meets Pod Security `restricted`: `runAsNonRoot`, `allowPrivilegeEscalation: false`, drop `ALL`, `seccompProfile: RuntimeDefault`. | tool (SC-K8S-PRIVILEGED, Trivy config) + judge | A02 |
| CK-5 | `readOnlyRootFilesystem: true` with an `emptyDir` on `/tmp`. `automountServiceAccountToken: false` when the app does not call the API server. | judge | A02 |
| CK-6 | Secrets come from an external store, not from a `ConfigMap` or a base64 `Secret` in Git. | judge | CWE-798 |
| CK-7 | A default-deny NetworkPolicy exists, with explicit allows and DNS. | judge | A02 |

## LLM calls

Only when the code calls a model. Detail: `llm-apps.md`.

| # | Check | Type | Maps to |
|---|-------|------|---------|
| LL-1 | User text and document text stay out of the system prompt and are marked as data. | judge | LLM01 |
| LL-2 | Model output is validated or encoded before HTML, SQL, a path, a URL or a tool call. | judge | LLM10 |
| LL-3 | Tools run with the rights of the user, not of the app. Destructive tools need a human confirmation. | judge | LLM03 |
| LL-4 | The retrieval context has a tenant filter. No secrets in prompts. | judge | LLM02, LLM08, LLM09 |
| LL-5 | Each call has `MaxOutputTokens`, a timeout, a rate limit per user and a budget. | judge | LLM06 |
| LL-6 | Model output that changes a legal or financial value (customs value, HS code) has a validation or a human check. | judge | LLM07 |

## Design

| # | Check | Type | Maps to |
|---|-------|------|---------|
| DS-1 | A design that adds a trust boundary answers the five STRIDE questions (`threat-model.md`). | judge | A06, ASVS V15 |
| DS-2 | Each security decision that the change needs has an accepted ADR (SEC IDs). | judge | ASVS V15 |

## Analyzer IDs

These rules come from the .NET SDK and EF Core. This skill does not repeat them in `rules.json`. Turn them on with `AnalysisModeSecurity=All`.

| ID | Finds |
|----|-------|
| CA2100 | SQL command text from a non-constant string |
| CA2300, CA2301 | BinaryFormatter deserialization |
| CA2326 to CA2330 | Newtonsoft `TypeNameHandling` and missing binder |
| CA2254 | Log message template that is not constant |
| CA3012 | Regex injection (data flow) |
| CA3075, CA3076 | Insecure DTD processing, XSLT script |
| CA5350, CA5351 | Weak (TripleDES, SHA1) and broken (MD5, DES, RC2) algorithms |
| CA5358 | Unsafe cipher mode (ECB) |
| CA5359 | Disabled certificate validation |
| CA5390 | Hard-coded encryption key |
| CA5394 | Insecure randomness (`System.Random`) |
| EF1002 | Interpolated string in a raw SQL method |
| EF1003 | Concatenated string in a raw SQL method |
| SYSLIB0011 | BinaryFormatter use (error in .NET 9 and later) |
| ASPDEPR005 | Obsolete `KnownNetworks` (use `KnownIPNetworks`) |
