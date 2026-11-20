# Research: dotnet-mcp, api-design, and webmcp

Date: 2026-11-19. Status: research for phase 2 of the stack split. Nothing in this file was compiled or run.

This file gives the facts for 3 new skills: `dev-stack:dotnet-mcp`, `dev-stack:api-design`, and `frontend-stack:webmcp`. Each skill must stop two failures. The model writes an old pattern from its training data. Or the model does not know a newer technique.

Each fact has a tag:

- "verified, docs": a primary document (RFC editor, IETF datatracker, a GitHub repository of the owner of the standard or SDK, Microsoft Learn, chromestatus.com) says it.
- "verified, package metadata": the NuGet or npm registry says it.
- "not verified": only a secondary source says it, or nobody could confirm it.

## Contents

- dotnet-mcp
- api-design
- webmcp
- Facts that are not verified

## dotnet-mcp

### Versions and status

| Item | Value | Date | Tag |
|------|-------|------|-----|
| `ModelContextProtocol` | 2.4.0 (latest), 2.3.1, 1.4.2 (servicing line) | 2.4.0: 2026-11-07 | verified, package metadata |
| `ModelContextProtocol.AspNetCore` | 2.4.0 | 2026-11-07 | verified, package metadata |
| `ModelContextProtocol.Core` | 2.4.0 | 2026-11-07 | verified, package metadata |
| First stable release | 1.0.0 | 2026-02-25 | verified, package metadata |
| First 2.x release | 2.0.0 | 2026-07-28 | verified, package metadata |
| Target frameworks of 2.4.0 | net10.0, net9.0, net8.0, netstandard2.0 | 2026-11-07 | verified, package metadata |
| `Microsoft.McpServer.ProjectTemplates` | 1.2.1 | 2026-05-08 | verified, package metadata |
| MCP specification | 2026-07-28 (latest). Earlier: 2025-11-25, 2025-06-18, 2025-03-26, 2024-11-05 | 2026-07-28 | verified, docs |
| server.json schema (MCP Registry) | 2025-12-11 is the newest dated schema. Learn and the template still show 2025-10-17 | 2025-12-11 | verified, docs |

Sources:

- NuGet flat container and registration API: `https://api.nuget.org/v3-flatcontainer/modelcontextprotocol/index.json` (and the `.aspnetcore`, `.core` IDs).
- SDK releases: https://github.com/modelcontextprotocol/csharp-sdk/releases (v1.0.0, v2.0.0, v2.3.0, v2.4.0).
- Specification changelog: https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/changelog.mdx
- server.json changelog: https://github.com/modelcontextprotocol/registry/blob/main/docs/reference/server-json/CHANGELOG.md

The SDK is stable. The 2.x line is the current major version. 2.0.0 aligns the SDK with the 2026-07-28 specification. The SDK still talks to peers on 2025-11-25 and earlier. (verified, docs: v2.0.0 release notes)

### What the 2026-07-28 specification changed

Source for each item: the specification changelog and the deprecated-features registry (`docs/specification/2026-07-28/deprecated.mdx`). Tag: verified, docs.

- No protocol sessions. The `Mcp-Session-Id` header is gone from Streamable HTTP. A server that needs state between calls gives the client an explicit handle as a tool argument (SEP-2567).
- No `initialize` handshake. Each request carries the protocol version and the client capabilities in `_meta`. Servers must implement `server/discover` (SEP-2575).
- `subscriptions/listen` replaces the HTTP GET stream and `resources/subscribe`.
- `ping`, `logging/setLevel`, and `notifications/roots/list_changed` are removed. The log level is now set for each request in `_meta`.
- Multi round-trip requests (MRTR) replace the server-to-client requests for elicitation, sampling, and roots. The server returns `resultType: "input_required"`, and the client sends the request again with `inputResponses` (SEP-2322).
- Each result has a required `resultType` field.
- Tasks moved out of the core protocol into the extension `io.modelcontextprotocol/tasks` (SEP-2663).
- SSE resumability (`Last-Event-ID`) is removed. A broken stream loses the request, and the client sends it again with a new ID.
- POST requests need the headers `Mcp-Method` and `Mcp-Name` (SEP-2243).
- List results and `resources/read` need `ttlMs` and `cacheScope` (SEP-2549).
- `inputSchema` and `outputSchema` accept any JSON Schema 2020-12 keyword. `structuredContent` can be any JSON value (SEP-2106).
- The "resource not found" error code changed from `-32002` to `-32602` (SEP-2164).
- Deprecated, with earliest removal in the first revision on or after 2027-07-28: Roots, Sampling, Logging, and Dynamic Client Registration. Client ID Metadata Documents replace Dynamic Client Registration.
- The HTTP+SSE transport (deprecated since 2025-03-26) is now formally in the Deprecated state (SEP-2596).

Authorization (2026-07-28, `basic/authorization/index.mdx`, verified, docs):

- The MCP server is an OAuth 2.1 resource server. It must publish OAuth 2.0 Protected Resource Metadata (RFC 9728).
- A 401 response has `WWW-Authenticate: Bearer resource_metadata="..."` and should have a `scope` parameter.
- Clients must send the `resource` parameter (RFC 8707). Servers must check that the token audience is the server itself. Token passthrough to an upstream API is not allowed.
- Clients must check the `iss` parameter (RFC 9207) when it is present.

### C# API (2.4.0)

| Topic | Current API | Source | Tag |
|-------|-------------|--------|-----|
| Package choice | `ModelContextProtocol` for stdio and clients. `ModelContextProtocol.AspNetCore` for HTTP. `ModelContextProtocol.Core` for the minimum dependencies | csharp-sdk `docs/concepts/getting-started.md` | verified, docs |
| Stdio server | `Host.CreateApplicationBuilder`, `AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly()` | same | verified, docs |
| Logs in stdio | `builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace)` | same, and the template `local/Program.cs` | verified, docs |
| HTTP server | `AddMcpServer().WithHttpTransport(...)`, then `app.MapMcp()` (or `MapMcp("/mcp")`) | `docs/concepts/transports/transports.md` | verified, docs |
| Session mode | `HttpServerTransportOptions.SessionMode`, default `HttpServerSessionMode.Stateless`. Also `Stateful` and `StatefulForInitializeClients` (hybrid) | `HttpServerTransportOptions.cs` at tag v2.4.0, `docs/concepts/mrtr/mrtr.md` | verified, docs |
| Old switch | `Stateless` (bool) still exists as an alias of `SessionMode`. Default was `false` in 1.4.2 and `true` from 2.0.0 | the same file at tags v1.4.2, v2.0.0, v2.4.0 | verified, docs |
| Tools | `[McpServerToolType]` on the class, `[McpServerTool, Description("...")]` on the method, `[Description]` on each parameter | getting-started, tools.md | verified, docs |
| DI into tools | Parameters of type `McpServer`, `IProgress<ProgressNotificationValue>`, `ClaimsPrincipal`, `CancellationToken`, or a registered service are injected. They are not in the input schema | `docs/concepts/tools/tools.md` | verified, docs |
| Tool hints | `ReadOnly`, `Destructive`, `Idempotent`, `OpenWorld`, `Title` on `[McpServerTool]`. Defaults: `ReadOnly = false`, `Destructive = true`, `OpenWorld = true` | `McpServerToolAttribute.cs` at v2.4.0 | verified, docs |
| Structured output | `UseStructuredContent = true`, optional `OutputSchemaType`. From 2.0.0, a non-object return value is sent as is (`72`), not as `{ "result": 72 }` | tools.md, v2.0.0 release notes | verified, docs |
| Tool errors | An exception becomes `CallToolResult` with `IsError = true`. Only an `McpException` message reaches the client. Other exceptions give a generic message | tools.md | verified, docs |
| Client input | Throw `InputRequiredException` (MRTR). Check `server.IsMrtrSupported` first. `ElicitAsync`, `SampleAsync`, `RequestRootsAsync` throw in stateless mode | `docs/concepts/mrtr/mrtr.md` | verified, docs |
| Auth | `AddAuthentication` with `DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme`, `AddJwtBearer` with `ValidAudience` set to the server URL, `.AddMcp(o => o.ResourceMetadata = new() { AuthorizationServers = {...}, ScopesSupported = [...] })`, then `app.MapMcp().RequireAuthorization()` | `samples/ProtectedMcpServer/Program.cs` | verified, docs |
| Per-tool auth | `[Authorize]` on tools after `AddAuthorizationFilters()` | `docs/concepts/identity/identity.md` | verified, docs |
| Host check | Set `AllowedHosts` to known names, not `"*"`, against DNS rebinding. Enable CORS only for a known browser origin | getting-started | verified, docs |
| Legacy SSE | Off by default. `EnableLegacySse` is `[Obsolete]` (MCP9004) and needs `SessionMode = Stateful` | transports.md, `list-of-diagnostics.md` | verified, docs |
| Diagnostics | MCP9005: Roots, Sampling, Logging are deprecated. MCP9006: stateful-only options. MCP9007: old OAuth redirect delegate. MCPEXP003: MCP Apps. MCPEXP005: MCP Skills (2.4.0) | `docs/list-of-diagnostics.md` | verified, docs |

### Template and NuGet packaging

- `dotnet new install Microsoft.McpServer.ProjectTemplates`, then `dotnet new mcpserver -n Name`. The template needs the .NET 10 SDK. It has a stdio (local) and an HTTP (remote) option, and options for native AOT and self-contained publish. (verified, docs: https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server)
- The project has `<PackageType>McpServer</PackageType>` and a `.mcp/server.json` file. NuGet.org reads only the first `packages` entry with `registryType: "nuget"`. Declare inputs in `environmentVariables` and `packageArguments`. (verified, docs: same page. Verified, package metadata: template content)
- `dotnet pack -c Release` makes one tool package and one package for each runtime identifier. Push all of them. Clients start the server with `dnx <PackageId>@<version> --yes`. `dnx` ships with the .NET 10 SDK. (verified, docs: same page)
- Template 1.2.1 references `ModelContextProtocol` 1.2.0 and `ModelContextProtocol.AspNetCore` 1.2.0. Its HTTP `Program.cs` sets `options.Stateless = true`. Its server.json uses schema 2025-10-17. (verified, package metadata: the content of the template nupkg)
- Learn still labels the template "preview". NuGet has 1.2.1 as a stable version. (verified, docs and package metadata)

### Scope boundary

- `dev-stack:aspnetcore` owns the host, middleware order, CORS wiring, and JWT bearer setup. `dotnet-mcp` owns only what MCP adds: `AddMcpServer`, transports, `MapMcp`, tool attributes, the `.AddMcp(...)` resource metadata, and MCP diagnostics.
- `dev-stack:secure-coding` owns token validation in general, secrets, SSRF from tools, and prompt injection in LLM calls. `dotnet-mcp` gives the MCP-specific rules: audience check, no token passthrough, `AllowedHosts`, tool hints for destructive tools.
- `dev-stack:dotnet-observability` owns logging and OpenTelemetry. `dotnet-mcp` says only: in stdio, logs go to stderr. Do not use the MCP Logging feature for new code.
- A client of an MCP server inside an app (Microsoft.Extensions.AI `IChatClient` with `McpClientTool`) is in scope only as a reference.

### Top traps

| No | Old pattern | Current pattern | Source |
|----|-------------|-----------------|--------|
| M1 | Package version `0.x-preview` or 1.x, from the template or from training data | `ModelContextProtocol` 2.4.0. After `dotnet new mcpserver`, update the 1.2.0 references | NuGet, template nupkg |
| M2 | HTTP+SSE transport: `/sse` and `/message` endpoints, or `EnableLegacySse = true` | Streamable HTTP through `MapMcp()`. SSE only for an old client, with MCP9004 suppressed and a reason | spec deprecated.mdx, transports.md |
| M3 | Stateful sessions by default, sticky load balancing, `Mcp-Session-Id` logic | Stateless (the 2.x default). An explicit handle as a tool argument when state is necessary | spec changelog SEP-2567, transports.md |
| M4 | `Console.WriteLine` or default console logging in a stdio server (stdout breaks the protocol) | `LogToStandardErrorThreshold = LogLevel.Trace`. Nothing else on stdout | spec `transports/stdio.mdx`, getting-started |
| M5 | `server.ElicitAsync` or `SampleAsync` from a tool on an HTTP server | `InputRequiredException` (MRTR) after an `IsMrtrSupported` check. Sampling: call the LLM provider API directly | mrtr.md, deprecated.mdx |
| M6 | MCP Logging (`notifications/message`, `logging/setLevel`) for diagnostics | `ILogger` to stderr or OpenTelemetry | deprecated.mdx (SEP-2577) |
| M7 | HTTP server with no auth, or JWT with `ValidateAudience = false`, or the client token sent to an upstream API | `.AddMcp(...)` resource metadata, audience = server URL, `RequireAuthorization()` on `MapMcp`, an own token for upstream calls | authorization/index.mdx, ProtectedMcpServer sample |
| M8 | A delete or write tool with no hints, so the client sees it as safe | `ReadOnly = true` on read tools. `Destructive = true` stays on write tools. `Idempotent = true` where true | McpServerToolAttribute.cs |
| M9 | A client that reads `structuredContent.result` | Read the value by the advertised output schema | v2.0.0 release notes |

### Candidate eval scenarios

E-MCP-1. Stdio server for NuGet.

- Task: "Make a .NET MCP server that a client starts with `dnx`. It has a tool `get_order_status(orderId)` that reads from an injected `IOrderStore`, and a tool `cancel_order(orderId)`. Write `Program.cs`, the tool class, the `.csproj`, and `.mcp/server.json`."
- Script checks, new pattern: `ModelContextProtocol` version `^2\.`, `WithStdioServerTransport`, `LogToStandardErrorThreshold`, `<PackageType>McpServer</PackageType>`, `ReadOnly\s*=\s*true` on the read tool, `registryType.*nuget` in server.json.
- Script checks, old pattern: `Console\.WriteLine` in a tool or `Program.cs`, version `0\.\d+\.\d+-preview` or `1\.\d`.

E-MCP-2. Remote HTTP server with OAuth.

- Task: "Expose the same tools over HTTP for a remote client. Tokens come from an existing OpenID Connect provider. The service runs on 3 nodes behind a load balancer."
- Script checks, new pattern: `WithHttpTransport`, `MapMcp`, `RequireAuthorization`, `McpAuthenticationDefaults`, `ResourceMetadata`, `ValidAudience`.
- Script checks, old pattern: `EnableLegacySse`, `MapSse` or a route `"/sse"`, `SessionMode\s*=\s*HttpServerSessionMode\.Stateful`, `Stateless\s*=\s*false`, `ValidateAudience\s*=\s*false`.

## api-design

### Versions and status

| Standard | Status | Date | Source | Tag |
|----------|--------|------|--------|-----|
| RFC 9457 Problem Details | Proposed Standard. Obsoletes RFC 7807 | 2023-08 | https://www.rfc-editor.org/rfc/rfc9457 | verified, docs |
| RFC 9110 HTTP Semantics | Internet Standard. Obsoletes RFC 7231 and RFC 7232 (conditional requests) | 2022-06 | https://www.rfc-editor.org/rfc/rfc9110 | verified, docs |
| RFC 9111 HTTP Caching | Internet Standard. Obsoletes RFC 7234 | 2022-06 | https://www.rfc-editor.org/rfc/rfc9111 | verified, docs |
| RFC 6585 (428, 429) | Proposed Standard | 2012-04 | https://www.rfc-editor.org/rfc/rfc6585 | verified, docs |
| RFC 10008 HTTP QUERY method | Proposed Standard (safe, idempotent method with a body) | 2026-06 | https://www.rfc-editor.org/rfc/rfc10008 | verified, docs |
| RFC 9745 Deprecation header | Proposed Standard | 2025-03 | https://www.rfc-editor.org/rfc/rfc9745 | verified, docs |
| RFC 8594 Sunset header | Informational | 2019-05 | https://www.rfc-editor.org/rfc/rfc8594 | verified, docs |
| RFC 8288 Web Linking (`Link`) | Proposed Standard | 2017-10 | https://www.rfc-editor.org/rfc/rfc8288 | verified, docs |
| RFC 7396 JSON Merge Patch | Proposed Standard | 2014-10 | https://www.rfc-editor.org/rfc/rfc7396 | verified, docs |
| RFC 6902 JSON Patch | Proposed Standard | 2013-04 | https://www.rfc-editor.org/rfc/rfc6902 | verified, docs |
| RFC 9651 Structured Fields | Proposed Standard. Obsoletes RFC 8941 | 2024-09 | https://www.rfc-editor.org/rfc/rfc9651 | verified, docs |
| RFC 9727 api-catalog | Proposed Standard | 2025-06 | https://www.rfc-editor.org/rfc/rfc9727 | verified, docs |
| Idempotency-Key header | IETF draft -08, WG document, active. Not an RFC | 2026-10-22, expires 2027-04-25 | https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/ | verified, docs |
| RateLimit and RateLimit-Policy headers | IETF draft -11, WG document. Not an RFC. The draft expires 2026-11-24 | 2026-05-23 | https://datatracker.ietf.org/doc/draft-ietf-httpapi-ratelimit-headers/ | verified, docs |
| OpenAPI | 3.2.1 is the latest (3.2.0: 2025-09-19). 3.1.2 is the latest 3.1 | 3.2.1: 2026-09-10 | https://github.com/OAI/OpenAPI-Specification/releases | verified, docs |

Content of the drafts (verified, docs: the draft text):

- Idempotency-Key -08: an Item Structured Header (RFC 9651). A missing key gives 400. The same key with another payload gives 422. A retry while the first request runs gives a conflict (409). The API must publish its key expiry policy. Errors use RFC 9457.
- RateLimit -11: two fields, `RateLimit-Policy: "default";q=100;w=60` and `RateLimit: "default";r=50;t=30`. They replace the `X-RateLimit-*` trio. The 429 status comes from RFC 6585, with `Retry-After`.

### Well-known guidelines

| Guideline | Current state | Notes for the skill | Tag |
|-----------|---------------|---------------------|-----|
| Microsoft REST API Guidelines | Repo `microsoft/api-guidelines`, branch `vNext`, last push 2026-10-30. Azure guidelines in `azure/Guidelines.md` | PATCH with JSON Merge Patch. PUT for create or replace. 202 and `Operation-Location` for long-running work. `If-Match` and ETag. POST must be idempotent (`Repeatability-Request-ID`). Own error envelope with `x-ms-error-code`, not RFC 9457 | verified, docs |
| Zalando RESTful API Guidelines | Repo `zalando/restful-api-guidelines`, last push 2026-09-29 | Must support RFC 9457 problem JSON (rule 176). Prefer cursor pagination (rule 160). Kebab-case path segments, snake_case query parameters and fields. Verb-free URLs. Media type versioning, no URL versioning. Still names the `X-RateLimit-*` headers | verified, docs |
| Google AIP | Repo `aip-dev/google.aip.dev`, last push 2026-11-17 | AIP-134 update with field mask, AIP-136 custom methods (`:verb`), AIP-151 long-running operations, AIP-155 request ID, AIP-158 page tokens, AIP-160 filtering, AIP-193 errors | verified, docs |

The 3 guidelines do not agree on errors, versioning, and case. The skill must not copy one guideline. It gives a default and a decision with options.

### Scope boundary

- `api-design` owns the contract:
  - resources, URLs, methods, and status codes
  - the error contract and the rate-limit headers
  - conditional requests and idempotency
  - pagination, filters, and sort order
  - partial updates and long-running work
  - the policy for versions and deprecation
- `dev-stack:aspnetcore` owns the wiring: `AddProblemDetails`, `TypedResults`, the rate limiter, the idempotency filter code, JSON options. Today its `references/decisions.md` holds API-04 (error contract), API-12 (pagination), API-13 (idempotency), API-17 (URL style), and API-03 (contracts). These are contract decisions. Recommendation: move the question and options of these decisions to `api-design`, and keep in `aspnetcore` only a pointer and the ASP.NET Core rule. Else the two skills say the same thing twice.
- `dev-stack:aspnetcore-openapi` owns the OpenAPI document, its tools, Asp.Versioning, and the oasdiff gate (API-09, API-10, API-18, API-19). `api-design` says only which version policy and which deprecation headers the contract has.
- `dev-stack:secure-coding` owns authentication and authorization. It also owns object-level access (BOLA), mass assignment, and the OWASP API Top 10 risks. `api-design` names these risks in the design checklist and points to `secure-coding`.

### Top traps

| No | Old pattern | Current pattern | Source |
|----|-------------|-----------------|--------|
| A1 | Cite RFC 7807, or invent an error envelope `{ "error": "..." }` | RFC 9457 `application/problem+json` with `type`, `title`, `status`, `detail`, `instance`, and a stable `code` extension | RFC 9457, Zalando rule 176 |
| A2 | `200 OK` with `{ "success": false }` | The correct 4xx or 5xx status with a problem body. 422 for a valid request that breaks a business rule, 409 for a state conflict | RFC 9110 sections 15.5.10, 15.5.21 |
| A3 | Verbs in URLs (`/getOrders`, `/createOrder`) | Plural nouns. An action that is not CRUD is `POST /orders/{id}/cancel` (or `:cancel` in Google style) | Zalando "verb-free URLs", AIP-136 |
| A4 | Offset pagination (`?page=500&size=50`) on a large or changing set | Opaque cursor (`?cursor=...&limit=50`), `next` link in the body or a `Link: <...>; rel="next"` header. Offset only for a small set with page numbers in the UI | Zalando rule 160, AIP-158, RFC 8288 |
| A5 | POST create with no retry safety | Client-supplied ID with PUT, or the `Idempotency-Key` header: 400 when missing, 422 on reuse with another payload, 409 while in progress | Idempotency-Key draft -08, Azure guidelines |
| A6 | Last write wins on PUT or PATCH | `ETag` on GET. `If-Match` on PUT, PATCH, DELETE. `412 Precondition Failed` on mismatch. `428 Precondition Required` when the header is missing | RFC 9110 section 13, RFC 6585 |
| A7 | PATCH with a full object, or JSON Patch for simple field updates | `application/merge-patch+json` (RFC 7396) as the default. JSON Patch (RFC 6902) only for array operations. Note: in merge patch, `null` deletes a field | RFC 7396, Azure guidelines |
| A8 | A long job inside one request until a timeout | `202 Accepted` with `Location` (or `Operation-Location`) of a status resource. The status resource returns the state and, at the end, a link to the result | Azure guidelines, AIP-151 |
| A9 | `X-RateLimit-Limit`, `X-RateLimit-Remaining`, `X-RateLimit-Reset` | 429 with `Retry-After`. Optional: the draft `RateLimit` and `RateLimit-Policy` fields, marked as a draft | RFC 6585, RateLimit draft -11 |
| A10 | Version end announced only in a changelog, or `Warning: 299` | `Deprecation` (RFC 9745, a structured date `@1767225600`) and `Sunset` (RFC 8594, an HTTP date), plus `Link: <...>; rel="deprecation"` | RFC 9745, RFC 8594 |
| A11 | A long filter in a GET query string, or a POST "search" that is not safe | GET with simple query parameters. For a complex query body: the `QUERY` method (RFC 10008) when the stack supports it, else `POST /orders/search` documented as safe | RFC 10008 |

### Candidate eval scenarios

E-API-1. Design an order API contract (framework independent).

- Task: "Write an OpenAPI 3.1 YAML file for an orders API. It lists the orders of a customer (up to 5 million rows) and gets one order. It creates an order (mobile clients retry on a timeout). It changes the delivery address and cancels an order. It has an export that takes minutes."
- Script checks, new pattern: `application/problem\+json`, `Idempotency-Key`, `If-Match`, `'412'`, `application/merge-patch\+json`, `cursor` or `page_token` or `pageToken`, `'202'`, a cancel path with POST.
- Script checks, old pattern: `7807`, paths with `/get`, `/create`, `/update`, `/delete`, a `page` plus `offset` parameter on the list path, `X-RateLimit`.

E-API-2. Review an existing contract.

- Task: "Review this contract and list each problem with a fix." The OpenAPI file has 8 planted mistakes:
  - 200 with an error flag, and verbs in paths
  - offset pagination on an export, and PUT for a partial update
  - no ETag, and `X-RateLimit-*` headers
  - RFC 7807 in a description, and no 429
- Check: a script counts the planted IDs in the answer. A judge grades the fixes.

## webmcp

Label: preview. The specification is a Community Group draft. Each API name in this section can change. The skill must use a feature check and progressive enhancement (decision Q18).

### Versions and status

| Item | Value | Date | Source | Tag |
|------|-------|------|--------|-----|
| Specification | "WebMCP", Status `CG-DRAFT`, Web Machine Learning Community Group. Not a W3C Standard | last commit 2026-11-13 | https://github.com/webmachinelearning/webmcp (`index.bs`) | verified, docs |
| Chrome developer trial | From Chrome 146, behind `chrome://flags/#enable-webmcp-testing` | n/a | chromestatus 5117755740913664, Chrome docs | verified, docs |
| Chrome origin trial | Chrome 149 to 156, extended to 162. Chrome status stays "Proposed" | entry updated 2026-11-11 | https://chromestatus.com/feature/5117755740913664 | verified, docs |
| Edge | Origin trial in Edge 150 | n/a | `implementation-status.md` in the spec repo | verified, docs |
| Firefox, Safari | Standards position requested. No implementation. Chrome status says "No signal" | n/a | same, and chromestatus | verified, docs |
| Brave (Leo AI chat) | WebMCP support expected in Brave 1.102.2 | expected 2027-01-05 | `implementation-status.md` | verified, docs |
| `@w3c/webmcp-polyfill` | 0.1.1 (first release 2026-11-03), from the W3C Open Source Program | 2026-11-16 | npm, https://github.com/w3c-oss/webmcp-polyfill | verified, package metadata |
| `webmcp-types` | 0.1.11 (TypeScript types) | 2026-11-12 | npm | verified, package metadata |
| `@mcp-b/global` (MCP-B) | 6.2.0. `@mcp-b/webmcp-polyfill` 6.2.0 says "Deprecated: use @w3c/webmcp-polyfill" | 2026-11-10 | npm | verified, package metadata |

### Current API shape

Source: `index.bs` and `README.md` in webmachinelearning/webmcp. Tag: verified, docs.

- Entry point: `document.modelContext`. It exists only in a secure context (`[SecureContext]`).
- `registerTool(tool, options)` returns a Promise. It rejects on a duplicate name, an empty name or description, or an invalid schema.
- The tool dictionary has `name`, `title`, `description`, `inputSchema`, `outputSchema`, `execute(input, { signal })`, and `annotations`.
- Annotations: `readOnlyHint`, `untrustedContentHint`, `consequentialHint`, `debugging`. All default to `false`.
- Options: `signal` (an `AbortSignal`: abort it to remove the tool) and `exposedTo` (a list of secure origins).
- `getTools({ fromOrigins })` and `executeTool(tool, input, { signal })` are for agents in the page or in a frame.
- Events: `toolchange`, `toolactivated`, `toolcancel`.
- Permissions Policy feature `tools`, default allowlist `self`. A cross-origin iframe needs `allow="tools"`. `Permissions-Policy: tools=()` turns the API off. Then `registerTool` rejects with `NotAllowedError`.

API history (verified, docs: commit log of `index.bs`):

| Date | Change |
|------|--------|
| 2026-02-19 | First IDL on `navigator.modelContext` |
| 2026-03-05 | `provideContext()` and `clearContext()` removed |
| 2026-03-26 | `registerTool()` takes an optional `AbortSignal` |
| 2026-05-27 | The `modelContext` getter moved from `Navigator` to `Document` |
| 2026-06-08 | `registerTool()` returns a Promise |
| 2026-06-11 | `ModelContextClient` (and with it `requestUserInteraction`) removed for now |
| 2026-07-09 | Chromium removed `navigator.modelContext` (WPT PR 61186, merged 2026-07-09) |
| 2026-10-08 | The declarative API (form attributes) removed from the specification for now |
| 2026-11-02 | `outputSchema` added |

The current IDL has no `unregisterTool()`. Abort the registration signal instead.

The declarative API is a separate explainer (`declarative-api-explainer.md`): `<form toolname tooldescription toolautosubmit>` and `toolparamdescription` on controls, and `SubmitEvent.respondWith()`. It is not in the specification. Chrome docs (updated 2026-10-07) still describe a "Declarative API" for HTML forms, and one Chrome demo uses it. (verified, docs)

### Feature check and progressive enhancement

The pattern that the sources support:

```js
const mc = window.isSecureContext ? document.modelContext : undefined;
if (mc && typeof mc.registerTool === "function") {
  const reg = new AbortController();
  try {
    await mc.registerTool({ name, description, inputSchema, execute }, { signal: reg.signal });
  } catch (e) {
    // NotAllowedError: Permissions Policy blocks "tools". The page works without tools.
  }
}
```

- The page works the same without WebMCP. A tool calls the same application function as the UI button. (verified, docs: README "reuse existing client-side application logic")
- The W3C polyfill does not replace a native `document.modelContext`, also not a partial one. Load it before tool registration. Its entry is `@w3c/webmcp-polyfill/auto` or `installWebMCP()`. (verified, docs: polyfill README)
- In a component framework, register in a mount effect and abort the signal on unmount. (verified, docs: polyfill README, Next.js section)
- Do not fall back to `navigator.modelContext` in new code. Chromium removed it on 2026-07-09. (verified, docs: WPT PR 61186.) The Chrome milestone of the removal is not verified.

### Security model

Tag: verified, docs (spec sections on permissions and security, Chrome docs).

- Secure context only. Same-origin frames get the API. A cross-origin frame needs `allow="tools"`.
- A tool is visible only to its own document, same-origin documents, and the built-in browser agent, unless `exposedTo` lists an origin.
- Chrome extensions can list and run tools through content scripts. Treat each tool call as a call from an untrusted actor: check permissions on the server, as for a click.
- Set `consequentialHint: true` on a tool that pays, sends, or deletes. Set `untrustedContentHint: true` when the output has user-generated text (prompt injection risk). Set `readOnlyHint: true` on read tools.
- The tool runs with the user session. The server API must still check authorization (`dev-stack:secure-coding`, `frontend-stack:web-security`).

### Scope boundary

- `frontend-stack:webmcp` owns the browser tool API, the feature check, the polyfill choice, tool design (names, schemas, hints), and tool lifecycle in a SPA.
- `frontend-stack:angular` owns the component code. `webmcp` gives only the register-on-init and abort-on-destroy rule.
- `frontend-stack:web-security` owns XSS, CSP, and token storage. Permissions Policy headers for `tools` go in both, with `web-security` as the owner of the header.
- `dev-stack:dotnet-mcp` owns server-side MCP. WebMCP is not an MCP transport. The page does not speak JSON-RPC.
- `frontend-stack:modern-web` routes general web work to Modern Web Guidance. Check that Modern Web Guidance has no WebMCP guide before the skill text is final. (not verified)

### Top traps

| No | Old pattern | Current pattern | Source |
|----|-------------|-----------------|--------|
| W1 | `navigator.modelContext` | `document.modelContext` | `index.bs` (2026-05-27), WPT PR 61186 |
| W2 | `navigator.modelContext.provideContext({ tools: [...] })` or `clearContext()` | One `registerTool()` call for each tool | `index.bs` (2026-03-05) |
| W3 | `unregisterTool(name)` | `AbortController`, `{ signal }` on `registerTool`, then `abort()` | `index.bs` IDL, README |
| W4 | `registerTool(...)` without `await` and without a catch | `await` the Promise. Catch `NotAllowedError` and invalid schema errors | `index.bs` (2026-06-08), README |
| W5 | Call the API with no feature check, so the page breaks in Firefox and Safari | Check `isSecureContext` and `document.modelContext`. The UI works without it | implementation-status.md, decision Q18 |
| W6 | `agent.requestUserInteraction()` in `execute` | Not in the current draft. Confirm in the page UI, and set `consequentialHint` | `index.bs` (2026-06-11) |
| W7 | Tools registered once at start for each screen of the app | Register tools for the active view. Abort them when the view leaves | README best practices |
| W8 | `@mcp-b/webmcp-polyfill` | `@w3c/webmcp-polyfill` | npm deprecation text |

### Candidate eval scenarios

E-WEB-1. Add WebMCP tools to a todo page.

- Task: "This plain JavaScript todo page has `addTodo(text)` and `listTodos()`. Let browser agents add and list todos. The page must still work in Firefox and Safari. Write the code in `webmcp.js`."
- Script checks, new pattern: `document\.modelContext`, `registerTool`, `AbortController` or `signal`, `readOnlyHint` on the list tool, a check such as `if\s*\(.*modelContext` or `isSecureContext`, `await` on `registerTool`.
- Script checks, old pattern: `navigator\.modelContext`, `provideContext`, `clearContext`, `unregisterTool`.

E-WEB-2. Tools in an Angular component.

- Task: "Expose a `checkout_cart` tool from the cart component, only while the cart view is open. Checkout charges a card."
- Script checks, new pattern: `registerTool` in `ngOnInit` or a constructor with `DestroyRef`, `abort\(\)` in `ngOnDestroy` or `onDestroy`, `consequentialHint:\s*true`.
- Script checks, old pattern: `navigator\.modelContext`, `unregisterTool`, registration in `main.ts` or `app.config.ts`.

## Facts that are not verified

- The Chrome milestone that removed `navigator.modelContext`. The Chromium commit is from 2026-07-09. Secondary sources name Chrome 150 as a deprecation. The Chrome release notes do not confirm it.
- Whether Chrome still ships the declarative form attributes now that the specification removed them (2026-10-08). The Chrome docs mention a Declarative API, but give no attribute names.
- Whether Modern Web Guidance has WebMCP content.
- Whether ASP.NET Core 10 or 11 supports the `QUERY` method (RFC 10008) in routing and in OpenAPI. Not checked.
- The end date of the Chrome origin trial as a calendar date. Only the milestones (149 to 162) are known.
- Whether a newer `Microsoft.McpServer.ProjectTemplates` that references SDK 2.x is in progress. NuGet shows 1.2.1 as the latest.
