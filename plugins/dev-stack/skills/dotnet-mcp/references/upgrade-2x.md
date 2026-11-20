# Upgrade from SDK 0.x or 1.x to 2.x

Read this file when a server is on `ModelContextProtocol` 0.x or 1.x. An existing server keeps its version until the task asks for the upgrade (stack rule 3). Then give the human the list of breaks first.

## Contents

- Version history
- What breaks at 2.0
- Removed and renamed types
- Steps

## Version history

| Version | Date | Note |
|---------|------|------|
| 0.x previews | 2025 | Unstable API. Many names changed |
| 1.0.0 | 2026-02-25 | First stable release |
| 1.4.2 | servicing line | Default `Stateless = false` |
| 2.0.0 | 2026-07-28 | Spec 2026-07-28. Default stateless. MRTR |
| 2.4.0 | 2026-11-07 | Current version |

## What breaks at 2.0

Most of these compile with no error. Each one changes the behavior at run time.

| Change | Effect on old code | Fix |
|--------|--------------------|-----|
| HTTP default is stateless (`Stateless` default `true`, `SessionMode` default `Stateless`) | Code that keeps state in the session loses it | A handle argument and a shared store, or `SessionMode` set explicitly after a decision (MCP-02) |
| `ElicitAsync`, `SampleAsync`, `RequestRootsAsync` throw `InvalidOperationException` in stateless mode | A tool that asks the user fails on each HTTP call | `InputRequiredException` after `IsMrtrSupported` (`SKILL.md` rule 14) |
| Spec 2026-07-28 deprecates Sampling, Roots, and Logging (MCP9005) | Warnings. Removal in a spec revision on or after 2027-07-28 | A direct LLM call, tool arguments, `ILogger` to stderr or OpenTelemetry |
| Stateful-only options are obsolete (MCP9006): `IdleTimeout`, `MaxIdleSessionCount`, `EventStreamStore`, `SessionMigrationHandler` | Warnings. With warnings as errors, the build fails | Remove them, or keep stateful mode after a decision |
| `EnableLegacySse` is obsolete (MCP9004) | Warning. The spec deprecates the HTTP+SSE transport | `MapMcp()` with Streamable HTTP |
| The SDK sends a non-object structured value as is (`72`), not as `{ "result": 72 }` | A client that reads `structuredContent.result` gets nothing | Read the value by the output schema |
| "Resource not found" error code is `-32602`, not `-32002` | A client that checks the old code misses the error | Check `-32602` |
| No `initialize` handshake and no `Mcp-Session-Id` header in spec 2026-07-28 | Proxy rules or logs that read the header see nothing | Remove the header logic |

The 2.x SDK still talks to clients on spec 2025-11-25 and earlier.

## Removed and renamed types

These names do not exist in 2.4.0. The build gives CS0246 or CS0103.

| Old | Current |
|-----|---------|
| `IMcpServer` | `McpServer` |
| `IMcpClient` | `McpClient` |
| `McpClientFactory.CreateAsync(...)` | `McpClient.CreateAsync(...)` |
| `McpServerFactory.Create(...)` | `McpServer.Create(...)` |
| `SseClientTransport` | `HttpClientTransport` |

## Steps

1. Read the version of each `ModelContextProtocol*` reference. Done when you have the list.
2. Tell the human which breaks of the previous table apply to the code. Then get the approval.
3. Set each reference to 2.4.0 in one change.
4. Build with `-warnaserror`. Fix each CS0246, MCP9004, MCP9005, and MCP9006. Done when the build has 0 warnings.
5. Search for `ElicitAsync`, `SampleAsync`, `RequestRootsAsync`, `Stateless`, `SessionMode`, and `structuredContent`. Fix each hit by the table.
6. Run the tests and the stackcheck of this skill. Done when both pass.
