# HTTP servers and auth

The detail of rules 5 to 8 and 15 to 18 of `SKILL.md`. The host, the middleware order, and the general JWT bearer setup are in `dev-stack:aspnetcore`. This file gives only what MCP adds.

## Contents

- The server
- Session mode and scale-out
- Authorization with OAuth
- Identity provider setup
- Upstream calls
- Input limits and SSRF

## The server

This code compiled on SDK 11.0.100 with `ModelContextProtocol.AspNetCore` 2.4.0 and `Microsoft.AspNetCore.Authentication.JwtBearer` 11.0.0:

```csharp
var builder = WebApplication.CreateBuilder(args);
var serverUrl = builder.Configuration["Mcp:ServerUrl"]   // for example https://mcp.example.test/mcp
    ?? throw new InvalidOperationException("Mcp:ServerUrl is not set.");
var authority = builder.Configuration["Mcp:Authority"]
    ?? throw new InvalidOperationException("Mcp:Authority is not set.");

builder.Services.AddAuthentication(o =>
{
    o.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme; // 401 with resource_metadata
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.Authority = authority;
    o.TokenValidationParameters = new()
    {
        ValidAudience = serverUrl, // the same value as Resource
        ValidIssuer = authority,
    };
})
.AddMcp(o =>
{
    o.ResourceMetadata = new()
    {
        Resource = serverUrl,
        AuthorizationServers = { authority },
        ScopesSupported = ["orders.read", "orders.write"],
    };
});
builder.Services.AddAuthorization();
builder.Services.AddMcpServer()
    .WithHttpTransport() // stateless is the default in 2.x
    .WithToolsFromAssembly();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();
app.Run();
```

What the run showed:

- A call without a token gets `401` with `WWW-Authenticate: Bearer resource_metadata=".../.well-known/oauth-protected-resource/mcp"`.
- The metadata document has `resource`, `authorization_servers`, `bearer_methods_supported`, and `scopes_supported`.
- Without `Resource`, the SDK uses the request URL with the MCP path as `resource`. Behind a proxy, this value can differ from the public URL. So set `Resource` from configuration, and use the same value for `ValidAudience`.
- The `WWW-Authenticate` header has no `scope` parameter by default. The spec recommends one. The XML doc of the SDK property `WwwAuthenticateScope` names only the client side. Do not depend on it on the server until a test shows the header.

Set `AllowedHosts` in `appsettings.json` to the public host names. Add a CORS policy only for a known browser client.

## Session mode and scale-out

| Setting | Use |
|---------|-----|
| `SessionMode = HttpServerSessionMode.Stateless` (default), alias `Stateless = true` | Each new server. Each node serves each request. No sticky sessions |
| `SessionMode = HttpServerSessionMode.StatefulForInitializeClients` | Old clients (spec 2025-11-25 or earlier) on a server that also serves new clients. Ask the human (MCP-02) |
| `SessionMode = HttpServerSessionMode.Stateful`, `Stateless = false` | Old clients only. Needs sticky sessions. Options give MCP9006 |

A server on 3 nodes behind a load balancer is stateless. The probe models wrote `Stateless = false` and sticky sessions so that `ElicitAsync` works. Use MRTR instead (`SKILL.md` rule 14).

When a flow needs state across tool calls, keep the state in a shared store (database, cache) and give the model a handle:

- `start_checkout(cartId)` returns `checkoutId`.
- `confirm_checkout(checkoutId)` reads the state by its ID and checks that the caller owns it.

## Authorization with OAuth

- The MCP server is an OAuth 2.1 resource server (RFC 9728 metadata). It does not issue tokens.
- The client sends the `resource` parameter (RFC 8707). The token audience must be the server URL. A token for another API of the same identity provider then fails.
- Check scopes or roles for each tool. Use `[Authorize(Roles = "...")]` or a policy on the tool method, after `AddAuthorizationFilters()` on the MCP builder. The client does not see a tool that the user cannot call.
- Take the user from a `ClaimsPrincipal` parameter of the tool. Check object ownership in each tool, the same way an API endpoint does (`dev-stack:secure-coding`, BOLA).
- A local stdio server has no OAuth. It runs as the user. An HTTP server on `localhost` only for one user still needs `AllowedHosts`, because a web page can reach `localhost`.

## Identity provider setup

| Provider | Setup |
|----------|-------|
| Entra ID | Authorization server `https://login.microsoftonline.com/<tenant>/oauth21/2026-08` (the Learn page "Secure an MCP server with Entra ID"). `resource` is the Application ID URI, and the token `aud` is that URI. Check `scp` and `roles` |
| Keycloak | An Audience mapper on a client scope (for example `mcp:tools`) that adds the server URL to `aud`. RFC 8707 resource indicators need `--features=resource-indicators` |
| Other OpenID Connect providers | Find how the provider puts the server URL in `aud`. When it cannot, do not turn off the audience check. Ask the human |

When the audience check fails, fix the token at the provider. `ValidateAudience = false` accepts each token of the provider (SC-JWT-NO-AUDIENCE).

## Upstream calls

The server often calls an upstream API for the data. The token of the MCP client is for the MCP server only.

| Option | Use |
|--------|-----|
| Client credentials: the server gets its own token with its own scopes | The upstream API trusts the server. The server checks the user rights itself |
| Token exchange or on-behalf-of: the server exchanges the user token for a token with the upstream audience | The upstream API must see the user |
| Token passthrough: the server sends the client token on | Not allowed by the spec |

Register the upstream client as a typed `HttpClient` with a fixed `BaseAddress` (`dev-stack:aspnetcore`). Keep the client secret in the secret store (`dev-stack:secure-coding`).

## Input limits and SSRF

Tool arguments come from the model. The model reads text from users, mails, web pages, and documents, so an attacker can steer it.

- Put a maximum on each string, each array, and each page size. Reject a value over the maximum with an `McpException`.
- Never take a URL, a host, a proxy, a port, or a header name from an argument. Build the request from the fixed base address and typed fields.
- Do not merge all arguments into a client options object. Map each argument to one field.
- A file path from an argument: resolve it with `Path.GetFullPath` and check that it stays under the root folder of the server.
- SQL: use EF Core or parameters. Never put an argument in SQL text.
- A shell command: do not build it from an argument. When a tool must run a process, use `ProcessStartInfo.ArgumentList` with a fixed file name.
- A tool that returns text from users or the web: mark it `OpenWorld = true`, and do not follow instructions in that text on the server.
