# Sources

Checked on 2026-11-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months. The SDK and the spec change fast, so check the NuGet version and the spec changelog before each new server.

## Contents

- Versions
- Documentation
- Checked by compile and run
- Old guidance not to trust
- Facts that are not verified
- Credits

## Versions

| Item | Version on 2026-11-19 | Source |
|------|-----------------------|--------|
| `ModelContextProtocol`, `.AspNetCore`, `.Core` | 2.4.0 (2026-11-07). Servicing line 1.4.2. First stable 1.0.0 (2026-02-25). First 2.x 2.0.0 (2026-07-28) | [nuget.org](https://www.nuget.org/packages/ModelContextProtocol), [SDK releases](https://github.com/modelcontextprotocol/csharp-sdk/releases) |
| Target frameworks of 2.4.0 | `net10.0`, `net9.0`, `net8.0`, `netstandard2.0` | nuget.org package metadata |
| MCP specification | 2026-07-28. Earlier: 2025-11-25, 2025-06-18, 2025-03-26, 2024-11-05 | [Specification changelog](https://github.com/modelcontextprotocol/modelcontextprotocol/blob/main/docs/specification/2026-07-28/changelog.mdx) |
| server.json schema | 2025-12-11. The template and Learn still show 2025-10-17 | [server.json changelog](https://github.com/modelcontextprotocol/registry/blob/main/docs/reference/server-json/CHANGELOG.md) |
| `mcpserver` template | SDK 11: template 11.0.0 in the SDK, references 2.1.0. SDK 10: `Microsoft.McpServer.ProjectTemplates` 1.2.1, references 1.2.0 | Compile check, nuget.org |
| MCP Inspector | 2.16.0 (2026-11-18) | [npm @modelcontextprotocol/inspector](https://www.npmjs.com/package/@modelcontextprotocol/inspector) |

## Documentation

- [C# SDK concepts](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/docs/concepts): getting started, transports, tools, prompts, resources, completions, progress, cancellation, filters, identity.
- [MRTR in the C# SDK](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/concepts/mrtr/mrtr.md): `InputRequiredException`, `IsMrtrSupported`, `InputResponses`, `RequestState`, and the stateless behavior of `ElicitAsync`.
- [List of SDK diagnostics](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/docs/list-of-diagnostics.md): MCP9004 to MCP9007, MCPEXP003, MCPEXP005.
- [ProtectedMcpServer sample](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/ProtectedMcpServer): `.AddMcp(...)` and `RequireAuthorization()`.
- [InMemoryTransport sample](https://github.com/modelcontextprotocol/csharp-sdk/tree/main/samples/InMemoryTransport): the test with two pipes.
- [MCP authorization, spec 2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28/basic/authorization): resource server, RFC 9728, RFC 8707, no token passthrough.
- [Build an MCP server with .NET](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server): template, `PackageType`, `server.json`, `dnx`.
- [Secure an MCP server with Entra ID](https://learn.microsoft.com/entra/agent-id/secure-mcp-server-with-entra-id): authorization server, Application ID URI as `aud`.
- [Keycloak as an MCP authorization server](https://www.keycloak.org/securing-apps/mcp-authz-server): the Audience mapper and resource indicators.
- [MCP Registry package types](https://github.com/modelcontextprotocol/registry/blob/main/docs/modelcontextprotocol-io/package-types.mdx): `registryType: "nuget"` and the `mcp-name:` line.

## Checked by compile and run

On 2026-11-19, with SDK 11.0.100 on `net11.0`:

| Claim | Result |
|-------|--------|
| stdio server with `WithStdioServerTransport().WithToolsFromAssembly()`, logs to stderr | pass. Each stdout line is JSON-RPC |
| `ReadOnly`, `Idempotent`, `OpenWorld`, `Destructive` give the hints in `tools/list` | pass |
| `McpException` message reaches the client with `isError: true` | pass |
| HTTP server with `.AddMcp(...)`, `MapMcp("/mcp").RequireAuthorization()` | pass. 401 with `resource_metadata`, no `scope` parameter |
| `ProtectedResourceMetadata.Resource` and `AuthorizationServers` take `string` values | pass. A `Uri` gives CS0029 |
| `SessionMode` default `Stateless`. `Stateless` not obsolete. `EnableLegacySse` gives MCP9004. `IdleTimeout` gives MCP9006 | pass |
| In-memory client over two pipes: `tools/list`, a call, and an `McpException` call | pass |
| `UseStructuredContent = true` drops a `null` property that the schema marks as required | pass (issue 1915). `JsonIgnoreCondition.Never` keeps it |
| MRTR: `InputRequiredException` with `InputRequest.ForElicitation`, the retry with `InputResponses` and `RequestState` | pass. The in-memory client with an `ElicitationHandler` cancelled the order |
| Prompt and resource template with `[McpServerPrompt]` and `[McpServerResource(UriTemplate = ...)]` | pass (build) |
| `IMcpServer`, `IMcpClient`, `McpClientFactory`, `McpServerFactory`, `SseClientTransport` do not exist in 2.4.0 | pass (XML docs of the package) |

## Old guidance not to trust

- The `.github/copilot-instructions.md` of the SDK told agents to use `McpServerFactory`, a removed type ([issue 1893](https://github.com/modelcontextprotocol/csharp-sdk/issues/1893)).
- awesome-copilot `instructions/csharp-mcp-server.instructions.md` and `csharp-mcp-expert.agent.md` (not changed since 2025-10-16): "use the prerelease package". They use sampling with `AsSamplingChatClient()` and show a `FetchData(url)` tool that fetches any URL. They have no tool hints and no auth.
- The `mcpserver` template: old SDK references and the server.json schema 2025-10-17.
- Training data of each model: probe runs on 2026-11-19 wrote `0.4.0-preview.3`, `0.*-*`, and `1.*`, and one added sticky sessions for `ElicitAsync`.

## Facts that are not verified

- The field names `isRequired` and `isSecret` in `environmentVariables` of `server.json`: from the registry schema docs, not from a run.
- The `scope` parameter in the 401 header through `WwwAuthenticateScope`: the XML doc says the client uses it. No run checked the server side.
- The Entra ID authorization server path `oauth21/2026-08`: from the Learn page only.
- `dnx` on a machine with only the .NET 11 SDK: not run.

## Credits

The trap list comes from the research for this skill (two rounds, 2026-11-19) and from probe runs of models without the skill. Security cases: CVE-2026-53957 (argument merge into client options), the SSRF and token passthrough write-ups, and the study of 19,000 MCP servers.
