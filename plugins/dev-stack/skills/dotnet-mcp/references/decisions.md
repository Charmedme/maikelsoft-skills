# MCP decisions

The team decisions that an MCP server needs. The order is the order in which a new server meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

## Contents

- MCP-01. Transport
- MCP-02. HTTP session mode
- MCP-03. Authorization of an HTTP server
- MCP-04. Token for upstream calls
- MCP-05. Confirmation of a destructive tool
- MCP-06. Distribution
- MCP-07. Tool output

## MCP-01. Transport

- Question: Does the server run as a local process (stdio) or as a remote service (Streamable HTTP)?
- Ask when: a new server.
- Options: stdio: the client starts the process on the machine of the user, with the rights of the user. Streamable HTTP: one service for many users, behind auth. Both: one tool library with two hosts.
- Recommended: stdio when the data and the rights are those of one user on one machine (files, a local tool, a personal token). Streamable HTTP when the data lives on a server, many users share it, or the team must control access and logs.
- Choose another option when: the same tools must serve a desktop client and a web client. Then put the tools in a class library, and make two small hosts.
- Depends on: none.
- Revisit when: the server gets users on other machines.
- Check: judge.
- Detect: DET-MCP-01.

## MCP-02. HTTP session mode

- Question: Does the HTTP server run stateless, or stateful for old clients?
- Ask when: a new HTTP server, or a request for sessions, sticky sessions, or `ElicitAsync`.
- Options: `Stateless` (the 2.x default): each node serves each request. `StatefulForInitializeClients`: stateless for new clients, sessions for clients on spec 2025-11-25 or earlier. `Stateful`: sessions for each client, sticky sessions on the load balancer, MCP9006 warnings.
- Recommended: Stateless. Keep state in a shared store and give the model a handle. Ask the user through MRTR.
- Choose another option when: a client that the team must support speaks only spec 2025-11-25 or earlier, and needs elicitation. Then `StatefulForInitializeClients`, with sticky sessions for those clients only.
- Depends on: MCP-01.
- Revisit when: the last old client moves to spec 2026-07-28.
- Check: tool (MCP-HTTP-STATEFUL), judge.
- Detect: DET-MCP-02.

## MCP-03. Authorization of an HTTP server

- Question: How does the HTTP server check who calls it?
- Ask when: a new HTTP server.
- Options: OAuth 2.1 resource server with the existing identity provider, `.AddMcp(...)` metadata, and the audience check. An API key in a header: not in the MCP spec, and clients cannot discover it. No auth: only for a server that listens on `localhost` with `AllowedHosts` set.
- Recommended: OAuth 2.1 resource server. Set `Resource` and `ValidAudience` to the server URL, and `RequireAuthorization()` on `MapMcp`. MCP clients discover the identity provider through the metadata, and the audience check stops tokens of other APIs.
- Choose another option when: the identity provider cannot put the server URL in `aud`. Ask the human. Do not turn off the audience check.
- Depends on: MCP-01.
- Revisit when: the identity provider changes, or it gets RFC 8707 support.
- Check: tool (MCP-PRM-MANUAL, SC-JWT-NO-AUDIENCE), judge.
- Detect: DET-MCP-03.

## MCP-04. Token for upstream calls

- Question: Which token does the server send to an upstream API?
- Ask when: a tool calls an API that needs a token.
- Options: Client credentials: an own token of the server. Token exchange or on-behalf-of: a new token for the user, with the upstream audience. Token passthrough: the token of the MCP client.
- Recommended: Client credentials with the smallest scopes, and a user check in the tool. Token exchange when the upstream API must apply the rights of the user. Token passthrough is not allowed by the spec.
- Choose another option when: never for passthrough.
- Depends on: MCP-03.
- Revisit when: the upstream API changes its auth.
- Check: judge.
- Detect: none.

## MCP-05. Confirmation of a destructive tool

- Question: How does the human confirm a tool that cancels, deletes, pays, or sends?
- Ask when: the first tool with `Destructive = true` that the user cannot undo in one step.
- Options: MRTR elicitation that the client shows to the human. No destructive tool: the tool prepares the change, and the human finishes it in the app. Only the client prompt that the annotations cause: the client decides, and some clients have an "always allow" setting. A `confirm` argument or a returned token: the model can complete it alone, so it is no confirmation.
- Recommended: MRTR elicitation, with a text fallback when `IsMrtrSupported` is `false`. For a payment or another step with high cost, prepare only, and let the human finish in the app.
- Choose another option when: the action is cheap to undo (an archive with restore). Then the annotations and an audit log can be enough. Ask the human.
- Depends on: MCP-02.
- Revisit when: the spec adds a stronger user-consent feature.
- Check: judge.
- Detect: DET-MCP-05.

## MCP-06. Distribution

- Question: How do users get a stdio server?
- Ask when: a new stdio server.
- Options: A NuGet package for `dnx` with `<PackageType>McpServer</PackageType>` and `.mcp/server.json`. A self-contained executable for each platform. A container image. Source code that each user builds.
- Recommended: A NuGet package on the feed of the team (or nuget.org for a public server), started with `dnx <PackageId>@<version> --yes`. The client pins the version. Publish to the MCP Registry only when the human wants a public listing.
- Choose another option when: the users have no .NET SDK. Then a self-contained package (the template settings) or a container image.
- Depends on: MCP-01.
- Revisit when: the clients of the team get a new install method.
- Check: judge.
- Detect: DET-MCP-06.

## MCP-07. Tool output

- Question: Does a tool return text or structured content with an output schema?
- Ask when: the first tool that returns data.
- Options: Text: a sentence or a table for the model. Structured content: `UseStructuredContent = true`, typed JSON with an output schema. Both: structured content and a text summary.
- Recommended: Structured content for records and lists that a program or the model processes. Text for prose. Keep each `null` property in the JSON (`SKILL.md` rule 11).
- Choose another option when: the clients of the team do not read `structuredContent`. Then text.
- Depends on: none.
- Revisit when: never.
- Check: judge.
- Detect: DET-MCP-07.
