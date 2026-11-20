# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Package and project
- Transport
- Tools
- Confirmation and user input
- Auth and security
- Packaging
- Tests
- Decisions
- Severity
- Do not flag

## Package and project

| # | Check | Type |
|---|-------|------|
| 1 | Each `ModelContextProtocol*` reference is 2.x with an exact version. No `0.*-*`, `1.*`, or prerelease. | tool (MCP-PKG-OLD) |
| 2 | After `dotnet new mcpserver`, the references are 2.4.0, not the 2.1.0 or 1.2.0 of the template. | tool (MCP-PKG-OLD), judge |
| 3 | No removed types (`IMcpServer`, `McpServerFactory`, `McpClientFactory`). | tool (build) |
| 4 | The build has no MCP9004, MCP9005, or MCP9006 warning, or a reason on each suppression. | tool (build) |

## Transport

| # | Check | Type |
|---|-------|------|
| 5 | A stdio server writes nothing to stdout: no `Console.Write`, logs to stderr with `LogToStandardErrorThreshold`. | tool (MCP-STDOUT) |
| 6 | HTTP is stateless. No `Stateless = false`, no `SessionMode = Stateful`, no sticky sessions, without decision MCP-02. | tool (MCP-HTTP-STATEFUL) |
| 7 | No legacy SSE (`EnableLegacySse`, `/sse`, `/message`). | tool (MCP-HTTP-STATEFUL, MCP9004) |
| 8 | State across calls is an explicit handle argument, kept in a shared store. | judge |
| 9 | `AllowedHosts` lists the real host names. CORS only for a known origin. | judge |

## Tools

| # | Check | Type |
|---|-------|------|
| 10 | Each tool and each parameter has a `[Description]` with the format and an example. | judge |
| 11 | Read tools have `ReadOnly = true`. Write tools keep `Destructive = true` unless they only add. `Idempotent` and `OpenWorld` match the behavior. | judge |
| 12 | Parameters have specific types (`DateOnly`, enum, `Guid`), not a free dictionary or a JSON string. | judge |
| 13 | Each async tool takes a `CancellationToken` and passes it on. Long work reports progress. | judge |
| 14 | Errors for the model are `McpException` with a useful text, without a stack trace, SQL, or a secret. | judge |
| 15 | Structured output keeps each `null` property that the schema marks as required. | judge |

## Confirmation and user input

| # | Check | Type |
|---|-------|------|
| 16 | No `ElicitAsync`, `SampleAsync`, `RequestRootsAsync`, or `AsSamplingChatClient` in a tool on HTTP. | tool (MCP-SERVER-REQUEST) |
| 17 | Each destructive tool has an MRTR elicitation, with a check of `IsMrtrSupported` and of `RequestState`. | judge |
| 18 | No `confirm` argument and no returned token as the confirmation. | judge |
| 19 | When MRTR is not supported, the tool does not do the action. | judge |

## Auth and security

| # | Check | Type |
|---|-------|------|
| 20 | An HTTP server uses `.AddMcp(...)` metadata, not a hand-written `/.well-known/oauth-protected-resource` endpoint. | tool (MCP-PRM-MANUAL) |
| 21 | `ValidAudience` equals `ResourceMetadata.Resource`, and the audience check is on. | tool (SC-JWT-NO-AUDIENCE), judge |
| 22 | `MapMcp` has `RequireAuthorization()`. Tools with more rights have `[Authorize]` with `AddAuthorizationFilters()`. | judge |
| 23 | No client token goes to an upstream API (MCP-04). | judge |
| 24 | No URL, host, proxy, path, or SQL text from a tool argument. Arguments map to single fields. | judge |
| 25 | Each string, array, and page size has a maximum. | judge |
| 26 | Each tool checks that the user owns the object. | judge |

## Packaging

| # | Check | Type |
|---|-------|------|
| 27 | A stdio package has `<PackageType>McpServer</PackageType>` and packs `.mcp/server.json`. | judge |
| 28 | `server.json` uses schema 2025-12-11, `registryType: "nuget"` in the first entry, and the same version as the package. | tool (MCP-SERVERJSON-SCHEMA), judge |
| 29 | No secret value in `server.json`. Secret inputs have `isSecret`. | judge |
| 30 | For the MCP Registry, the README has the `mcp-name:` line. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 31 | Each tool has a test through an in-memory client: success, `McpException`, and the input limits. | judge |
| 32 | Each destructive tool has an accept test and a decline test. | judge |
| 33 | An HTTP server has a 401 test without a token and with a wrong audience. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 34 | New code follows the accepted MCP decisions, or the output marks them as assumed. | judge |

## Severity

These findings are blocking:

- A destructive tool that the model can complete alone (16 to 19).
- Token passthrough (23).
- A URL or a host from an argument (24).
- An HTTP server without the audience check (21).
- Stdout writes in a stdio server (5).

## Do not flag

These patterns are correct. A finding against them is a false positive.

- `o.Stateless = true` or no session setting. `Stateless` is not obsolete in 2.4.0, and stateless is the default.
- `server.ElicitAsync(...)` in a stdio-only server. It works on stdio. Prefer MRTR, but this is minor.
- `Console.Error.WriteLine(...)` in a stdio server. stderr is free for logs.
- No `scope` parameter in the 401 header. The SDK does not add it by default. Mention it as minor at most.
- `Destructive = true` on a tool that also has `Idempotent = true`. A cancel is both.
