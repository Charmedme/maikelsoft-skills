# Research round 2: AI mistakes and useful options for dotnet-mcp, api-design, and webmcp

Date: 2026-11-19. Status: research for phase 2 of the stack split. This file adds to `research-api-mcp-webmcp.md` (round 1). It does not repeat the traps M1 to M9, A1 to A11, and W1 to W8 of round 1.

The window for "recent" is 2026-05-19 to 2026-11-19. A report from before the window has the note "before window". It stays in the table only when it is still the current guidance.

Marks:

- "verified, source": the author read the primary source (the issue, the advisory, the package, the docs page).
- "not verified": only a secondary source or a search snippet says it.

## Contents

- dotnet-mcp
- api-design
- webmcp
- Probe method and cost
- New traps for the skills
- What this round could not verify

## dotnet-mcp

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| The SDK's own `.github/copilot-instructions.md` told coding agents to use `McpServerFactory`. The SDK team removed that type. Agents that read the file write code that does not compile | GitHub Copilot and each agent that reads the file | 2026-09-26 | https://github.com/modelcontextprotocol/csharp-sdk/issues/1893 | verified, source |
| The community instructions for Copilot say "use the prerelease package" (`--prerelease`). They use sampling with `McpServer.AsSamplingChatClient()`. They show a `FetchData(url)` tool that fetches any URL (SSRF). No tool hints, no auth. The file has not changed since 2025-10-16 and is still the current version | GitHub Copilot (awesome-copilot `csharp-mcp-server.instructions.md`, agent `csharp-mcp-expert.agent.md`) | file read 2026-11-19 (before window, still current) | https://github.com/github/awesome-copilot/blob/main/instructions/csharp-mcp-server.instructions.md | verified, source |
| Repositories with signs of AI-generated code are 42.6% of the MCP servers with a confirmed exploitable flaw. Classes: SQL injection 26%, command injection to RCE 22.5%, path traversal 19%, auth bypass 7.5% | AI coding tools in general (trait-based detection) | 2026-05-27 | https://www.trendaisecurity.com/it/resources-insights/deep-research/hunt-them-all-an-ai-powered-vulnerability-sweep-of-19-000-mcp-servers | verified, source |
| A tool merged all LLM-supplied arguments into the client options. The LLM could set `host` and `proxy`, and the server sent its own token to that host (CVE-2026-53957) | Not known (human or AI) | 2026-08-19 | https://securelayer7.net/lab/cve-2026-53957-contentful-mcp-server-ssrf-host-proxy-pat-exfiltration | verified, source |
| A generic `api_request` tool had no URL allowlist and no session auth. Result: SSRF. The server sent its credentials to any host (CVE-2026-15583 and a follow-up) | Not known | 2026-09-03 | https://labs.cloudsecurityalliance.org/research/csa-research-note-grafana-mcp-ssrf-session-spoofing-20260903/ | not verified |
| Token passthrough: the downstream API checked the token but not its audience | CTF write-up, not AI-specific | 2026-05-07 (before window) | https://bishopfox.com/blog/otto-support-ssrf-token-passthrough-with-mcp | verified, source |
| Taint-style flaws (input from the LLM goes into a shell, a path, or SQL) are a large share of MCP server flaws. Fixes are slow | Research | 2026-07-08 | https://arxiv.org/abs/2607.07461 | verified, source (abstract only) |
| With `UseStructuredContent`, the default serializer drops a null property that `outputSchema` marks as required. A client that validates the output rejects the result | SDK 2.x behaviour that generated code hits | 2026-10-10 | https://github.com/modelcontextprotocol/csharp-sdk/issues/1915 | verified, source |
| `WithTools<T>()` keeps a disposed service provider alive (memory leak in tests that build many hosts) | SDK behaviour | 2026-09-29 | https://github.com/modelcontextprotocol/csharp-sdk/issues/1898 | verified, source |

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku | Stdio server for `dnx` with `get_order_status` and `cancel_order` (E-MCP-1) | `ModelContextProtocol` version `0.*-*` (a floating preview). No `ReadOnly`, `Destructive`, or `Idempotent` hints on any tool. server.json schema 2025-10-17 (the newest is 2025-12-11). No `mcp-name:` line for the registry |
| Sonnet | Same | Version `0.4.0-preview.3`. server.json schema 2025-10-17. Correct: `LogToStandardErrorThreshold`, `ReadOnly = true` on the read tool, `Destructive = true` on cancel |
| Haiku | HTTP server, OIDC tokens, 3 nodes behind a load balancer, cancel must ask the user (E-MCP-2) | `ModelContextProtocol.AspNetCore` `0.4.0-preview.3`. Old alias `options.Stateless = true`. Hand-written `/.well-known/oauth-protected-resource` endpoint and a custom `OnChallenge` header, not `.AddMcp(o => o.ResourceMetadata = ...)`. The "confirmation" is a token that the tool gives to the model. The model sends it back. Thus the model can confirm by itself, and no human confirms |
| Sonnet | Same | Version `1.*`. Sets `o.Stateless = false` and tells the operator to use sticky sessions on `Mcp-Session-Id`, so that `server.ElicitAsync` works (traps M3 and M5 of round 1). Correct: `McpAuthenticationDefaults`, `.AddMcp(... ResourceMetadata ...)`, `RequireAuthorization()` |

Summary: both models write a 0.x or 1.x package version. Neither model knows the 2.x defaults (stateless, MRTR). The newer model is the one that adds sticky sessions.

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| Prompts: `[McpServerPromptType]`, `[McpServerPrompt]` | Give the user reusable prompt templates (slash commands in the client) | Stable (SDK 2.4.0) | csharp-sdk `docs/concepts/prompts/prompts.md` |
| Resources and resource templates: `[McpServerResource(UriTemplate = "docs://articles/{id}")]` (RFC 6570). Throw `McpException` for "not found" | Give read-only context (files, records) that the user or host attaches, not a tool call | Stable. Note: resource-not-found code is `-32602` from spec 2026-07-28 | `docs/concepts/resources/resources.md` |
| Completions: `WithCompleteHandler(...)` returns `CompleteResult` | Auto-complete for prompt arguments and resource template variables | Stable | `docs/concepts/completions/completions.md` |
| Structured output: `UseStructuredContent = true`, `OutputSchemaType` | A tool result that a client or a program reads as typed JSON | Stable. Mark required properties carefully (issue 1915) | `docs/concepts/tools/tools.md` |
| Progress: `IProgress<ProgressNotificationValue>` parameter | Long tool calls that report progress. Works in stateless and stdio mode | Stable | `docs/concepts/progress/progress.md` |
| Cancellation: `CancellationToken` parameter | Stop work when the client cancels the request | Stable | `docs/concepts/cancellation/cancellation.md` |
| MRTR: throw `InputRequiredException`, check `server.IsMrtrSupported` | Ask the user for input (confirm a cancel) on a stateless HTTP server | Stable (spec 2026-07-28) | `docs/concepts/mrtr/mrtr.md` |
| Filters: `WithRequestFilters(f => f.AddCallToolFilter(...))`, `WithMessageFilters(...)` | Audit log, per-tool authorization, rate limits for each tool | Stable | `docs/concepts/filters.md` |
| `[Authorize]` on tools with `AddAuthorizationFilters()` | Hide and block tools by role or scope | Stable | `docs/concepts/identity/identity.md` |
| Entra ID: authorization server `https://login.microsoftonline.com/<tenant>/oauth21/2026-08`, `resource` = Application ID URI = expected `aud`, check `scp` and `roles` | Protect a remote server with Entra ID | Stable docs. Client ID Metadata Documents in Entra: preview | https://learn.microsoft.com/entra/agent-id/secure-mcp-server-with-entra-id |
| Keycloak: an Audience mapper on a client scope (for example `mcp:tools`) with the server URL. RFC 8707 resource indicators only with `--features=resource-indicators` | Protect a remote server with Keycloak | Resource indicators and CIMD: experimental in Keycloak | https://www.keycloak.org/securing-apps/mcp-authz-server |
| MCP Registry: server.json schema 2025-12-11, `registryType: "nuget"`, and the line `mcp-name: <server name>` in the package README (ownership check) | Publish a NuGet MCP server to the registry with `mcp-publisher` | Stable (registry v1.8.9) | https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/package-types.mdx |
| MCP Inspector `npx @modelcontextprotocol/inspector` | Manual test of tools, resources, prompts, and the OAuth flow | Stable (2.16.0, 2026-11-18) | npm, https://modelcontextprotocol.io/docs/develop/build-with-agent-skills |
| In-memory transport: `StreamServerTransport` and `StreamClientTransport` over `System.IO.Pipelines.Pipe`, `McpClient.CreateAsync` | Unit tests of tools through the real protocol, without a process or HTTP | Stable | `samples/InMemoryTransport/Program.cs` |
| Microsoft.Extensions.AI: `McpServerTool.Create(AIFunction)`, and on the client `McpClientTool` is an `AIFunction` for `IChatClient` | Share one tool definition between an app agent and an MCP server | Stable (`Microsoft.Extensions.AI` 11.0.0) | `docs/concepts/resources/resources.md`, NuGet |
| MCP Apps: `ModelContextProtocol.Extensions.Apps` 2.4.0, `[McpAppUi(ResourceUri = "ui://...")]`, `WithMcpApps()`, MIME type `text/html;profile=mcp-app` | A tool that shows an HTML UI in the chat client | Experimental (`MCPEXP003`). Extension spec ext-apps v2.1.0 | `docs/concepts/apps/apps.md` |
| Tasks extension: `ModelContextProtocol.Extensions.Tasks` 2.4.0, `WithTasks` before other call-tool filters | Long-running tool calls that the client polls | Extension (`io.modelcontextprotocol/tasks`) | `docs/concepts/tasks/tasks.md`, `filters.md` |
| Skills extension: `ModelContextProtocol.Extensions.Skills`, `WithSkillsFromSubdirectories` | Serve agent skills from an MCP server | Experimental (`MCPEXP005`). NuGet delisted the package for a short time (issue 1998) | `docs/concepts/skills/skills.md` |
| Reference skills for agents: `mcp-server-dev` plugin (`build-mcp-server`, `build-mcp-app`, `build-mcpb`) | Compare the scope of `dotnet-mcp`. These skills are not C#-specific | Published on the MCP site | https://modelcontextprotocol.io/docs/develop/build-with-agent-skills |

## api-design

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| 4 AI app builders got the same FastAPI user API prompt. Findings: 17 endpoints with no authentication, 11 cross-user reads after login, and a users list with no object check (811 profiles). Also a page-size parameter with no maximum (whole table with password hashes), mass assignment of `role`, and a writable audit log | Emergent, Replit, Lovable, Base44 | 2026-11-03 | https://www.getastra.com/blog/?p=49968 | verified, source (vendor test) |
| An AI agent (Claude through OpenClaw) cancelled another customer's gym booking. The cancel endpoint had no object-level check (BOLA), and only the front end checked the booking window | Claude in an agent | 2026-08-10 | https://pasqualepillitteri.it/pl/news/10463/agent-ai-anulowal-cudza-rezerwacje-api (from an RNZ and ABC News report) | not verified (secondary report) |
| Agents that build a REST API from a fixed contract lose on average 27.28 points of assertion pass rate when the structural requirements increase. Data-layer defects are the main cause | Several LLM agents (models not named) | 2026-05-07, v2 2026-09-18 | https://arxiv.org/abs/2605.06445 | verified, source (abstract only) |
| 2,450 documentation and REST smells in 600 endpoints. Smells that confuse an agent: action URIs, POST for reads, loose parameters, undocumented status codes and error shapes, unclear security | Detector study (Hermes) | 2026-05-14 (before window) | https://arxiv.org/abs/2605.14312 | verified, source (abstract only) |
| Spectral 1.23.x rulesets need `spectral-formats` 1.9.0 for `oas3_2`. Example rules give false positives on OpenAPI 3.2 `dataValue` and `serializedValue` | Lint tool, hits generated 3.2 files | 2026-11-02, 2026-11-18 | https://github.com/stoplightio/spectral/issues/3100, https://github.com/stoplightio/spectral/issues/3110 | verified, source |

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku | Orders API (E-API-1), with rate limits and a v1 that retires next year | `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` (an old draft form). The current draft -11 has `RateLimit` and `RateLimit-Policy`. Deprecation only in text, no header schema. Correct: RFC 9457, `Idempotency-Key` (422 and 409), `If-Match` and 412, cursor, 202 export, `POST .../cancel` |
| Sonnet | Same | `openapi: 3.0.3` and `nullable: true` (not 3.1 or 3.2). `Deprecation` header with the example `'true'` (an old draft). RFC 9745 uses a structured date (`@1767225600`). Same old `RateLimit-*` trio. Correct: 428 and 412, cursor, 202, `Sunset` |
| Haiku | Product search with nested filters and 200 IDs, conflict error, webhook for price changes | `POST /products/search`, with no word on safety or the `QUERY` method. Webhook signature `X-Signature` = HMAC of the body only: no timestamp, so a replay is possible. Correct: merge patch, ETag, 412 and 428, `webhooks:` in 3.1 |
| Sonnet | Same | `POST /products/search` (documented as safe, but no `QUERY`). Webhook signs `{timestamp}.{body}` with a 5-minute window (good), but with own header names, not the Standard Webhooks names. Correct: cursor bound to the filter, merge patch, 412 |

Summary: the models know RFC 9457, idempotency keys, ETags, and cursors. They do not know the current RateLimit draft, the RFC 9745 header format, OpenAPI 3.2, or the `QUERY` method. The security mistakes (BOLA, no maximum page size, mass assignment) come from the reports, not from these contract probes.

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| `QUERY` method (RFC 10008) and the `query:` operation key of OpenAPI 3.2 | A safe, idempotent search with a request body | RFC: Proposed Standard (2026-06). OpenAPI 3.2: released. Framework support: check before use | https://www.rfc-editor.org/rfc/rfc10008, https://github.com/OAI/OpenAPI-Specification/releases/tag/3.2.0 |
| OpenAPI 3.2 features: `query`, `additionalOperations`, `in: querystring`, `itemSchema` for `text/event-stream` and `application/jsonl`, tag `parent` and `kind`, `$self` | A contract for streams, a full query string as one object, nested docs | 3.2.0 2025-09-19, 3.2.1 2026-09-10 | OpenAPI release notes |
| JSON Schema 2020-12 in OpenAPI 3.1 and 3.2: `type: [string, "null"]`, not `nullable` | Correct schemas for client generators | Stable | OpenAPI 3.1 and 3.2 |
| Spectral CLI 6.17.1 with the `spectral:oas` ruleset. The OWASP ruleset `@stoplight/spectral-owasp-ruleset` 2.0.1 | Lint a contract in CI, plus OWASP API Top 10 checks (no maximum on arrays, no 401 or 429, no auth) | Spectral: stable, supports 3.2 since 2026-03. OWASP ruleset: last release 2024-03 | npm, https://github.com/stoplightio/spectral/issues/2910 |
| Redocly CLI 2.82.0 (`redocly lint`, `recommended` rules, own rules) | Lint and bundle, supports 3.2 and Arazzo | Stable | npm, Redocly issues 3236 and 2563 |
| oasdiff (breaking-change check) | Gate for breaking changes in CI | Stable | owned by `aspnetcore-openapi` |
| Design-first: write the OpenAPI file, lint it, mock it, then build | Contract agreed before code, for agent and human clients | Practice | Redocly and Spectral docs |
| AsyncAPI 3.1.0 (spec), `@asyncapi/cli` 6.3.1 | Describe events and messages (the broker side of a webhook or an event) | Stable | https://github.com/asyncapi/spec/releases |
| Standard Webhooks 1.3.0: headers `webhook-id`, `webhook-timestamp`, `webhook-signature`, HMAC-SHA256 over `id.timestamp.body`, secret rotation | Signed webhooks with replay protection | Stable (community spec, 2026-11-17) | https://github.com/standard-webhooks/standard-webhooks |
| Problem type registry: one `type` URI for each error, a page that explains it, a stable `code` extension | Error contract that clients can switch on | RFC 9457 section 4 (IANA registry for common types) | RFC 9457 |
| ETag strategy: strong ETag from a row version for `If-Match`, weak ETag for cache validation, 428 when `If-Match` is missing | Concurrency control and caching | Stable (RFC 9110, RFC 6585) | RFC 9110 section 8.8.3 |
| Bulk operations. One request has many items, up to a maximum count. The response has a result for each item (in the body, or in a job with 202) | Imports and mass updates | Practice (Google AIP-233 and AIP-234, Azure guidelines) | https://google.aip.dev/233 |
| Field masks: `update_mask` or a `fields` parameter | Partial reads and partial updates without merge patch | Google AIP-161, AIP-157 | https://google.aip.dev/161 |
| Maximum page size and maximum array length in each schema | Stop the "whole database in one page" flaw | Practice, OWASP API4:2023 | Astra report, OWASP API Top 10 2023 |
| Agent-ready descriptions: an `operationId`, a summary, each status code, and the error shape for each operation | APIs that agents (MCP, tool calling) call | Research (Hermes study) | https://arxiv.org/abs/2605.14312 |

## webmcp

### Recent mistakes

| Mistake | Model or tool | Date | Source | Mark |
|---------|---------------|------|--------|------|
| An agent called a proposal tool, then clicked the page's own "Approve" button. The page recorded "approved by you", but the human did not decide | ChatGPT in-app browser (GPT-5.6) | 2026-09-03 | https://github.com/webmachinelearning/webmcp/issues/288 | verified, source (one run) |
| Google's own WebMCP skill marked a navigation tool `consequentialHint: true`. The audit was withdrawn: the hint is only for payments, bookings, deletes, and messages | Agent with the `build-webmcp-tools` skill | 2026-09-29 | https://github.com/GoogleChromeLabs/webmcp-tools/issues/511 | verified, source |
| Agents that followed the skill made search tools `readOnlyHint: true` background `fetch()` calls. The user saw no result on screen | Coding agents with `build-webmcp-tools` | 2026-10-08 | https://github.com/GoogleChromeLabs/webmcp-tools/issues/552 | verified, source |
| Agents registered a tool that needs one route's DOM globally. On other pages the tool did nothing and gave no error. Entry tools registered only on one page cost the agent a navigation turn | Coding agents with `build-webmcp-tools` | 2026-10-08 | https://github.com/GoogleChromeLabs/webmcp-tools/issues/551 | verified, source |
| Strict coding agents refused to build a checkout tool, because the skill text said "hand off to the UI" and "mark consequential" at the same time | Coding agents with `build-webmcp-tools` | 2026-10-08 | https://github.com/GoogleChromeLabs/webmcp-tools/issues/549 | verified, source |
| `inputSchema` passed as a JSON string. Current Chrome rejects it | Site code (often generated) | page checked 2026-11-17 | https://agentic-readiness.lumar.io/docs/capabilities/web-mcp | verified, source |
| Chrome returns a string result unquoted and `undefined` as `'undefined'`. The spec rejects `undefined`. A tool that returns nothing works in Chrome but fails in the spec and the polyfill | Chrome 158 against the spec | 2026-10-23 | https://github.com/webmachinelearning/webmcp/issues/361 | verified, source |
| `getTools()` gives `inputSchema` as a string in Chrome, an object in the draft. A test stub that follows the IDL passes, Chrome fails | Chrome origin trial against the draft | 2026-09-03 | https://github.com/webmachinelearning/webmcp/issues/287 | verified, source |
| Gemini in Chrome 157 does not see page tools. A test with "ask Gemini to call my tool" gives a false failure | Gemini in Chrome | 2026-11-18 | https://github.com/webmachinelearning/webmcp/issues/388 | verified, source |
| Of 580 sites that register tools, many use both `navigator.modelContext` and `document.modelContext`, and 67 ship the code but register nothing | Site code | 2026-09-23 | https://github.com/webmachinelearning/webmcp/issues/319 | verified, source |
| Hidden characters (Unicode tags, zero-width, bidi controls) in a tool description or schema text get past a review and a keyword guardrail | Threat note | 2026-10-21 | https://github.com/webmachinelearning/webmcp/issues/360 | verified, source |

### Probe results

| Model | Prompt | What was old or wrong |
|-------|--------|-----------------------|
| Haiku | Todo page tools that still work in Firefox and Safari (E-WEB-1) | `navigator.modelContext` (W1). Fallback to `provideContext({ tools })` (W2). `registerTool` with no `await` and no `AbortController` (W3, W4). No `readOnlyHint` on `list_todos`. Correct: a feature check that returns early |
| Sonnet | Same | `navigator.modelContext` (W1). `registerTool` with no `await`: the sync `try` does not catch a rejected Promise (W4). No `AbortController` (W3). Correct: a feature check, `readOnlyHint: true` on `list_todos`. The first run of this prompt did not stop: it hit the output limit and cost $1.28. The second run cost $0.01 |
| Haiku | Angular 21 `CartComponent`, `checkout_cart` only while the view is open, checkout charges a card | Declares `Navigator.modelContext`. Calls `unregisterTool(name)` or a returned `unregister()` handle (neither exists). No `await`. No `consequentialHint`. "Consent" is a `confirm: true` input that the agent fills in itself |
| Sonnet | Same | `navigator.modelContext`, `unregisterTool(name)` in `DestroyRef.onDestroy` (W1, W3). `agent.requestUserInteraction(...)` for the card confirmation (W6, not in the current draft). `annotations: { readOnlyHint: false }` but no `consequentialHint`. No `await` on `registerTool`. Good: a confirm dialog in the page UI |

Summary: all 4 webmcp answers use `navigator.modelContext`. None uses `document.modelContext`, an `AbortSignal`, or `await` on `registerTool`. No answer sets `consequentialHint` on the card payment. Sonnet also wrote a fake `Write` tool call as text, because the probe had no tools.

### Useful options

| Option | Task | Status | Source |
|--------|------|--------|--------|
| Annotations `readOnlyHint`, `consequentialHint`, `untrustedContentHint`: `consequentialHint` only for pay, book, delete, send. Navigation is `readOnlyHint: false` with no `consequentialHint` | Correct hints for each tool | CG draft | `index.bs`, webmcp-tools issue 511 |
| Human confirmation in the page UI, with a check that the click is a real user click is not possible (issue 288). For a payment, the tool stops at a review screen and the human pays in the UI | Checkout and other consequential tools | Open problem in the spec | issues 288, 356, 549 |
| Angular 22: `provideExperimentalWebMcpTools([...])` in `app.config.ts` or in route `providers`, `declareExperimentalWebMcpTool(tool)` in an injection context, router option `withAutoCleanupInjectors()` (also `withExperimentalAutoCleanupInjectors`), `experimentalWebMcpTool` on a Signal Form | WebMCP in an Angular 22 app with lifecycle and DI handled by Angular | Experimental, from Angular 22.0.0. Not in Angular 21 | https://angular.dev/ai/webmcp, `@angular/core` 22.3.0 `core.d.ts` |
| Chrome DevTools: Application panel, WebMCP pane (tools, invocation log, schema errors, "Run tool") | Manual test and debug of tools | Experimental | https://developer.chrome.com/docs/devtools/application/webmcp |
| Chrome DevTools MCP server with `--categoryExperimentalWebmcp`: tools `list_webmcp_tools`, `execute_webmcp_tool` | Let a coding agent test the page tools | Experimental, off by default | same page |
| Model Context Tool Inspector extension | Show the tools of any page in a side panel | Community extension in the Chrome Web Store, needs `#enable-webmcp-testing` | https://github.com/beaufortfrancois/model-context-tool-inspector |
| WebMCP Evals CLI and `webmcp-skills` (`build-webmcp-tools`) | Eval that the agent calls the right tool for each user request | Not an official Google product | https://github.com/GoogleChromeLabs/webmcp-tools |
| `@w3c/webmcp-polyfill` 0.1.1 | `document.modelContext` in browsers without it | Preview | round 1 |
| `@mcp-b/webmcp-local-relay` 6.2.0 (iframe and localhost WebSocket) and `@mcp-b/transports` | Bridge page tools to a desktop MCP client | Community, not part of the spec. A localhost relay exposes tools to local processes: threat-model it | npm |
| `Origin-Agent-Cluster: ?1` response header | WebMCP outside Chromium (polyfill or future engines) needs an origin-keyed agent cluster | Open spec issue | https://github.com/webmachinelearning/webmcp/issues/318 |
| `outputSchema` and a JSON-serializable return value (never `undefined`) | Typed results that work in Chrome, the spec, and the polyfill | CG draft (added 2026-11-02) | issue 361, `index.bs` |

## Probe method and cost

- Command: `claude -p "<prompt>" --model <haiku|sonnet> --tools "" --disallowedTools Skill Agent Bash Read Edit Write WebFetch WebSearch --strict-mcp-config --setting-sources "" --no-session-persistence --max-budget-usd 0.6 --output-format json`. Run from an empty scratch folder, with each `CLAUDECODE` and `CLAUDE_CODE_*` variable unset. One call at a time.
- 6 prompts (2 for each skill), each on Haiku and Sonnet: 12 calls, and one retry. The prompts and the raw answers are in the scratch folder of this session (`probe-api/prompts.txt` and `probe-api/out/`). They are not in the repository.
- Total cost: $1.58 for 13 calls (12 prompts, and one retry). Haiku calls cost less than $0.01 each. Sonnet calls cost $0.01 to $0.08 each, except the run that did not stop ($1.28).
- Limit: one run for each prompt and model. A pattern seen once is a signal, not a rate.

## New traps for the skills

| No | Old or wrong | Correct | Skill |
|----|--------------|---------|-------|
| M10 | Instructions or examples with `McpServerFactory`, `--prerelease`, or `AsSamplingChatClient()` | `McpServer.Create` for a manual server. Stable package 2.4.0. MRTR or a direct LLM call, not sampling | dotnet-mcp |
| M11 | Floating versions `0.*-*`, `1.*`, or `0.4.0-preview.3` | An exact 2.x version (`2.4.0`), with Central Package Management | dotnet-mcp |
| M12 | `options.Stateless = false` plus sticky sessions on `Mcp-Session-Id`, to make `ElicitAsync` work | Stateless (default) and `InputRequiredException` (MRTR). The 2026-07-28 spec has no session header | dotnet-mcp |
| M13 | "Confirmation" by a token or a `confirm: true` argument that the model sends back | A real user step: MRTR elicitation that the client shows to the human, or a confirm in the UI. Mark the tool `Destructive = true` | dotnet-mcp, webmcp |
| M14 | Hand-written `/.well-known/oauth-protected-resource` endpoint and custom `WWW-Authenticate` | `.AddMcp(o => o.ResourceMetadata = ...)` with `McpAuthenticationDefaults`. The `resource` value equals the expected `aud` | dotnet-mcp |
| M15 | A tool that takes a URL, host, or proxy from the LLM, or merges all arguments into client options | Fixed host in code, an allowlist, and an explicit argument list. Name the SSRF rule of `secure-coding` | dotnet-mcp |
| M16 | server.json `$schema` 2025-10-17, and no `mcp-name:` in the README | Schema 2025-12-11. The line `mcp-name: <server name>` in the package README for the registry ownership check | dotnet-mcp |
| M17 | Keycloak tokens without the MCP server in `aud`, so the audience check fails or gets turned off | An Audience mapper on a client scope. RFC 8707 only with `--features=resource-indicators` | dotnet-mcp |
| A12 | `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` | 429 with `Retry-After`. Optional: draft -11 `RateLimit` and `RateLimit-Policy`, marked as a draft | api-design |
| A13 | `Deprecation: true` | `Deprecation: @<unix seconds>` (RFC 9745), with `Sunset` and `Link rel="deprecation"` | api-design |
| A14 | `openapi: 3.0.3` with `nullable: true` for a new contract | 3.1 or 3.2 with `type: [string, "null"]` | api-design |
| A15 | Webhook HMAC over the body only, own header names | Standard Webhooks: `webhook-id`, `webhook-timestamp`, `webhook-signature`, a tolerance window, secret rotation | api-design |
| A16 | `POST /search` with no word on safety, or a long GET query string | `QUERY` (RFC 10008, OpenAPI 3.2 `query:`) when the stack supports it, else a POST documented as safe and idempotent | api-design |
| A17 | A page-size or array parameter with no `maximum` | A `maximum` on each `limit` and each array, and a 400 problem when it is too large | api-design |
| A18 | An action endpoint (cancel, approve) that checks the object owner on GET but not on the action | The same object-level check on each method of the resource. Name BOLA in the design checklist | api-design |
| W9 | A tool that returns `undefined` or nothing | Return a JSON-serializable value, and declare `outputSchema` | webmcp |
| W10 | Angular 22 code with a manual `registerTool` in a component | `provideExperimentalWebMcpTools` in route providers with `withAutoCleanupInjectors()`, or `declareExperimentalWebMcpTool`. Manual code only for Angular 21 and earlier | webmcp |
| W11 | `consequentialHint: true` on a navigation or search tool | Only on pay, book, delete, send. A search changes the visible results, so it is not `readOnlyHint: true` when it updates the screen | webmcp |
| W12 | A test that asks Gemini in Chrome to call the tool | Test with the DevTools WebMCP pane, the Chrome DevTools MCP server, or the evals CLI | webmcp |
| W13 | "The page confirms with a button, so the agent cannot pay alone" | An agent that also automates the page can click the button. Put the money step behind a real human step that the agent cannot do (for example a payment sheet or a re-authentication) | webmcp |

Change to round 1: E-WEB-2 has the old-pattern check "registration in `app.config.ts`". This check is wrong for Angular 22. In Angular 22, `provideExperimentalWebMcpTools` in `app.config.ts` or in route providers is the official pattern. Keep the check only for an app on Angular 21 or earlier. Or keep the scenario "only while the cart view is open", and check for route providers.

## What this round could not verify

- The gym booking incident: only a secondary article. The RNZ and ABC News originals were not read.
- The Grafana MCP CVE details: only a CSA research note in search results.
- Whether a model wrote the code in the Contentful and Grafana flaws. The advisories do not say.
- The Trend Micro AI share (42.6%) uses a heuristic for "AI-generated". The authors say so.
- The arXiv studies: the author read only the abstracts.
- Whether the Standard Webhooks 1.3.0 header names changed from 1.0. The author checked only the release date.
- The Chrome flag names for the DevTools WebMCP pane: the docs say that the pane needs no flag. A search snippet names `#devtools-webmcp-support`.
- The author found no public issue that reports a mistake by Copilot, Cursor, or Codex in C# MCP code. The C# evidence is the stale guidance (issue 1893, awesome-copilot) and the probe.
