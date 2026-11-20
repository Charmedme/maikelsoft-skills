---
name: dotnet-mcp
description: MCP servers in .NET with the official C# SDK (ModelContextProtocol 2.x, MCP spec 2026-07-28). Covers stdio and Streamable HTTP, the stateless default, tools with DI, tool annotations, structured output, prompts and resources, progress, MRTR instead of ElicitAsync, a real user confirmation, OAuth resource metadata and the audience check, SSRF and input limits, dnx packages with server.json, in-memory tests, and the upgrade from SDK 0.x or 1.x. Use when code or a review touches ModelContextProtocol, AddMcpServer, WithStdioServerTransport, WithHttpTransport, MapMcp, McpServerTool, McpServerToolType, McpServerPrompt, McpServerResource, InputRequiredException, ElicitAsync, SampleAsync, .AddMcp resource metadata, PackageType McpServer, .mcp/server.json, or dotnet new mcpserver. Not for MCP servers in TypeScript or Python, MCP client settings of an editor or agent, WebMCP in a web page (frontend-stack:webmcp), or REST endpoints (dev-stack:api-design, dev-stack:aspnetcore).
metadata:
  verified: "2026-11-19"
  versions: "ModelContextProtocol 2.4.0, ModelContextProtocol.AspNetCore 2.4.0, MCP specification 2026-07-28, server.json schema 2025-12-11, .NET 10 and .NET 11 SDK, MCP Inspector 2.16.0"
---

# MCP servers in .NET

An MCP server gives tools, prompts, and resources to an AI client. This skill covers servers with the official C# SDK. The host, the middleware order, and the JWT bearer setup are in `dev-stack:aspnetcore`. Token validation in general, secrets, SSRF, and prompt injection are in `dev-stack:secure-coding`. Logs and traces are in `dev-stack:dotnet-observability`. Browser tools on a web page (WebMCP) are not an MCP transport: see `frontend-stack:webmcp`.

Do not trust old guidance files for this SDK. The SDK's own old `.github/copilot-instructions.md` names `McpServerFactory`, a removed type. The awesome-copilot file `csharp-mcp-server.instructions.md` says "use the prerelease package", uses sampling, and shows a tool that fetches any URL. Use this skill and the sources in `references/sources.md`.

## First

1. Read `references/stack-rules.md`.
2. Find the package versions of `ModelContextProtocol`, `ModelContextProtocol.AspNetCore`, and `ModelContextProtocol.Core` in `*.csproj` or `Directory.Packages.props`.
   - 2.x: use this file.
   - 0.x or 1.x: read "Existing code" first. Upgrade only when the task asks for it (stack rule 3).
   - A new project from `dotnet new mcpserver`: the template of SDK 11 references 2.1.0, and the NuGet template 1.2.1 on SDK 10 references 1.2.0. Update each reference to 2.4.0 at once.
3. Find the transport: `WithStdioServerTransport` (local process) or `WithHttpTransport` with `MapMcp` (remote). A new server needs decision MCP-01.
4. List each tool, with what it reads or changes, and where the data lives (database, upstream API, file system).

Done when you can name the SDK version, the transport, and the side effect of each tool.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Transport | stdio for a server on the machine of the user. Streamable HTTP for a shared or remote server | MCP-01 |
| HTTP session mode | Stateless (the 2.x default). State as an explicit handle argument. No sticky sessions | MCP-02 |
| HTTP auth | OAuth 2.1 resource server: `.AddMcp(...)` metadata, audience = the server URL, `RequireAuthorization()` on `MapMcp` | MCP-03 |
| Upstream calls | An own token for the server (client credentials or token exchange). No token passthrough | MCP-04 |
| Destructive tools | MRTR elicitation that the human answers. No MRTR: no destructive action through MCP | MCP-05 |
| Distribution | NuGet package for `dnx`, `<PackageType>McpServer</PackageType>`, `.mcp/server.json` | MCP-06 |
| Tool output | Structured output with an output schema for data. Text for prose | MCP-07 |

## Existing code

A server on SDK 0.x or 1.x keeps its version until the task asks for the upgrade. Each item gives wrong behavior after a move to 2.x, and most compile without an error. Read `references/upgrade-2x.md` before an upgrade.

| Defect or break | Rule |
|-----------------|------|
| `0.x-preview`, a floating `1.*`, or `--prerelease` in the docs of the repo | 1 |
| `Stateless = false` or sticky sessions on `Mcp-Session-Id` | 6 |
| `ElicitAsync`, `SampleAsync`, or `RequestRootsAsync` in a tool on HTTP: they throw in stateless mode | 14 |
| A client that reads `structuredContent.result`: from 2.0, a non-object value is not wrapped | 11 |
| `EnableLegacySse = true` or `/sse` and `/message` endpoints | 7 |
| `IMcpServer`, `IMcpClient`, `McpClientFactory`, `McpServerFactory`, `SseClientTransport`: removed | 1 |

New code that copies one of these patterns depends on the defect. Fix it, and name the fix under "Changes".

## Rules

### Package and project

1. Reference `ModelContextProtocol` 2.4.0 (stdio) or `ModelContextProtocol.AspNetCore` 2.4.0 (HTTP) with an exact version. Use Central Package Management when the repo has it. Not `0.*-*`, `1.*`, or `--prerelease`. Models write 0.x and 1.x from their training data.
2. A server runs on .NET 10 or .NET 11. Package 2.4.0 has `net8.0`, `net9.0`, `net10.0`, and `netstandard2.0` assets. A build on `net11.0` passed.

### stdio

3. In a stdio server, stdout carries only JSON-RPC. One stray line breaks the client. Send all logs to stderr. Never call `Console.Write` or `Console.WriteLine`, also not in a tool or a library that the tool calls.

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace); // each log line to stderr
builder.Services.AddSingleton<IOrderStore, SqlOrderStore>();
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

4. A stdio server runs with the rights of the user who starts it. Keep each path and command inside the folders that the server owns.

### Streamable HTTP

5. Use `WithHttpTransport()` and `app.MapMcp("/mcp")`. Wire authentication and authorization first (rule 15).
6. Keep the stateless default (`SessionMode = HttpServerSessionMode.Stateless`, or the alias `Stateless = true`). Spec 2026-07-28 has no protocol session and no `Mcp-Session-Id` header. Each node can serve each request, so the load balancer needs no sticky sessions. When a tool needs state across calls, return a handle (for example a cart ID). The next tool takes it as an argument.
7. Use Streamable HTTP only. `EnableLegacySse` is obsolete (MCP9004) and needs stateful mode. `Stateful` mode and its options (`IdleTimeout`, MCP9006) exist only for clients on spec 2025-11-25 or earlier. Ask the human before you turn it on (MCP-02).
8. Set `AllowedHosts` to the real host names, not `*`. Add CORS only for a known browser origin. This blocks DNS rebinding to a local server.

### Tools

9. Put `[McpServerToolType]` on the class and `[McpServerTool]` with a `[Description]` on each method. Give each parameter a `[Description]` with the format and an example. The model picks a tool and fills its arguments only from these texts. Inject services through the constructor or as parameters. The SDK also injects `McpServer`, `CancellationToken`, `IProgress<ProgressNotificationValue>`, `ClaimsPrincipal`, and `RequestContext<CallToolRequestParams>`. These are not in the input schema.
10. Set the annotations on each tool. The defaults are `ReadOnly = false`, `Destructive = true`, `OpenWorld = true`. A read tool without `ReadOnly = true` looks like a write to the client. Set `Idempotent = true` when a second call has no new effect. Set `OpenWorld = false` when the tool reaches only your own data.

```csharp
[McpServerToolType]
public sealed class OrderTools(IOrderStore store)
{
    [McpServerTool(Name = "get_order_status", ReadOnly = true, Idempotent = true, OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Get the status of one order by its ID.")]
    public async Task<OrderStatus> GetOrderStatus(
        [Description("The order ID, for example 10045.")] string orderId,
        CancellationToken cancellationToken)
        => await store.FindAsync(orderId, cancellationToken)
           ?? throw new McpException($"Order {orderId} not found."); // only an McpException message reaches the client
}
```

11. For data that a program reads, set `UseStructuredContent = true` (MCP-07). The SDK sends an output schema. The serializer drops a `null` property that the schema marks as required, so a strict client rejects the result. Put `[property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]` on each nullable property of the result type.
12. Throw `McpException` for an error that the model can act on ("Order 9 not found."). Other exceptions reach the client as a generic message, so no stack trace or SQL text leaks.
13. Take a `CancellationToken` in each async tool and pass it on. For long work, take `IProgress<ProgressNotificationValue>` and report progress. Both work in stdio and in stateless HTTP.

### User input and confirmation

14. To ask the user for input on HTTP, throw `InputRequiredException` (MRTR). Check `server.IsMrtrSupported` first. `ElicitAsync`, `SampleAsync`, and `RequestRootsAsync` throw `InvalidOperationException` in stateless mode. Spec 2026-07-28 deprecates Sampling, Roots, and MCP Logging (MCP9005). When the server needs an LLM, call the provider API directly.
14a. A destructive tool (cancel, delete, pay, send) needs a confirmation that only the human can give. A `confirm: true` argument is no confirmation. A token that the tool returns to the model is none either: the model sends it back alone. Use an MRTR elicitation that the client shows to the human (MCP-05). Read `references/tools.md` for the full flow and the fallback.

### Auth and security

15. Protect each HTTP server that is not bound to localhost only. Use `.AddMcp(o => o.ResourceMetadata = ...)` with `McpAuthenticationDefaults.AuthenticationScheme` as the challenge scheme. Do not write the `/.well-known/oauth-protected-resource` endpoint or the `WWW-Authenticate` header by hand. Set `Resource` to the server URL, and set `ValidAudience` to the same value. Then a token for another API of the same identity provider fails. Add `RequireAuthorization()` on `MapMcp`. Read `references/http-auth.md` for the code and the identity provider setup.
16. Never send the token of the client to an upstream API. Get an own token for the server (MCP-04). A passed-through token lets one API act with the rights of another.
17. Each tool argument comes from the model, and the model reads text from users and documents (prompt injection). So validate each argument like public input. Put a maximum on each string, array, and page size. Never take a URL, host, proxy, path, or SQL text from an argument. Keep the host in code or in an allowlist, and map each argument to one field of the request. Apply the SSRF rules of `dev-stack:secure-coding`.
18. Use `[Authorize]` on a tool with `AddAuthorizationFilters()` to hide and block it by role or scope.

### Prompts, resources, and packaging

19. Use a prompt (`[McpServerPromptType]`, `[McpServerPrompt]`) for a template that the user starts. Use a resource (`[McpServerResourceType]`, `[McpServerResource(UriTemplate = "orders://{orderId}")]`) for read-only context that the user attaches. Use a tool for an action that the model starts.
20. Pack a stdio server for `dnx` (MCP-06). Read `references/packaging-testing.md` before you write the `.csproj` or `.mcp/server.json`.

Detail and examples: `references/tools.md`, `references/http-auth.md`, and `references/packaging-testing.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| A project or namespace that ends in `McpServer`, for example `Orders.McpServer`, with a tool parameter of type `McpServer` | A name such as `Orders.Mcp`. Else `McpServer` resolves to the namespace, and the build fails with CS0118 |
| `<PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />`, `Version="1.*"`, `dotnet add package ModelContextProtocol --prerelease` | `Version="2.4.0"` |
| `Console.WriteLine(...)` in a stdio server | `ILogger` with `LogToStandardErrorThreshold = LogLevel.Trace` |
| `o.Stateless = false;` and sticky sessions on `Mcp-Session-Id` | The stateless default and a handle argument |
| `EnableLegacySse = true`, `app.MapGet("/sse", ...)` | `app.MapMcp("/mcp")` |
| `await server.ElicitAsync(...)` in a tool on HTTP | `throw new InputRequiredException(...)` after `server.IsMrtrSupported` |
| `server.AsSamplingChatClient()` to call an LLM | A direct `IChatClient` of the provider |
| `[McpServerTool] public Task DeleteOrder(string id, bool confirm)` | `Destructive = true` and an MRTR elicitation |
| `[McpServerTool] public Task<string> FetchUrl(string url)` | A fixed host in code, one typed argument for each field |
| `app.MapGet("/.well-known/oauth-protected-resource", ...)` | `.AddMcp(o => o.ResourceMetadata = new() { ... })` |
| `ValidateAudience = false` | `ValidAudience` = the server URL = `ResourceMetadata.Resource` |
| `http.DefaultRequestHeaders.Authorization = incomingToken` to an upstream API | An own token for the server (MCP-04) |
| `McpServerFactory.Create(...)`, `McpClientFactory.CreateAsync(...)` | `McpServer.Create(...)`, `McpClient.CreateAsync(...)` |

## Check

1. Build with warnings as errors:

   ```
   dotnet build -warnaserror
   ```

   Pass: 0 warnings. MCP9004, MCP9005, and MCP9006 show old transport options and deprecated features.

2. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

3. Run the tests. Each tool has a test through an in-memory client (`references/packaging-testing.md`). Pass: all tests green.
4. Optional: to test by hand, start the MCP Inspector with `npx @modelcontextprotocol/inspector`. Check `tools/list` (annotations, schemas), each tool call, and for HTTP the 401 with `resource_metadata`.

## Review checklist

The full list is in `references/review-checklist.md`.

1. `ModelContextProtocol*` 2.x with an exact version. tool (MCP-PKG-OLD)
2. No stdout writes in a stdio server. tool (MCP-STDOUT)
3. Stateless HTTP, no legacy SSE, no sticky sessions. tool (MCP-HTTP-STATEFUL, MCP9004, MCP9006)
4. No `ElicitAsync`, `SampleAsync`, or `RequestRootsAsync` in an HTTP tool. tool (MCP-SERVER-REQUEST)
5. Each tool has a description, parameter descriptions, and correct annotations. judge
6. Each destructive tool has a confirmation that only the human can give. Severity: blocking. judge
7. HTTP: `.AddMcp` metadata, audience check, `RequireAuthorization()`. tool (MCP-PRM-MANUAL, SC-JWT-NO-AUDIENCE), judge
8. No token passthrough, no URL or host from a tool argument, a maximum on each input. Severity: blocking. judge
9. Structured output: nullable required properties stay in the JSON. judge
10. `server.json` uses schema 2025-12-11. tool (MCP-SERVERJSON-SCHEMA)

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Also give:

- Under "Changes": a table row for each tool with its annotations (ReadOnly, Destructive, Idempotent, OpenWorld) and its confirmation step.
- Under "Packages": the SDK version before and after. After `dotnet new mcpserver`, name the template version (2.1.0 or 1.2.0) and the update to 2.4.0.
- Under "Decisions": at least MCP-01, and MCP-02 to MCP-04 for an HTTP server. Mark each one accepted or assumed.

## References

- `references/decisions.md`: read before new code needs an MCP decision. It has the question, the options, and the recommendation.
- `references/tools.md`: read when you write a tool, a prompt, or a resource, structured output, progress, or the MRTR confirmation.
- `references/http-auth.md`: read when you write or review an HTTP server, its auth, or its upstream calls.
- `references/packaging-testing.md`: read before you pack, publish, or test a server.
- `references/upgrade-2x.md`: read when the server is on SDK 0.x or 1.x.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources, the compile evidence, and the old guidance not to trust.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
