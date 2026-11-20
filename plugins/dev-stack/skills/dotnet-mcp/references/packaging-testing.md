# Packaging and testing

The detail of rule 20 and of the Check section of `SKILL.md`.

## Contents

- Start from the template
- The project file
- server.json
- Pack, publish, and start with dnx
- Tests with an in-memory client
- MCP Inspector

## Start from the template

- SDK 11: `dotnet new mcpserver -n Example.Orders.Mcp` works with no install. The template of SDK 11 references `ModelContextProtocol` 2.1.0 (local) and `ModelContextProtocol.AspNetCore` 2.1.0 (remote).
- SDK 10: `dotnet new install Microsoft.McpServer.ProjectTemplates` installs template 1.2.1, which references 1.2.0.
- On SDK 11, an installed template 1.2.1 does not change the result. The template of the SDK has precedence.

After the template, do these steps:

1. Update each `ModelContextProtocol*` reference to 2.4.0.
2. Change the `$schema` of `.mcp/server.json` to 2025-12-11.
3. Replace each placeholder of `server.json` and the `.csproj`.

Done when the build passes and no file has a `<your ... here>` text.

## The project file

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackAsTool>true</PackAsTool>
    <PackageType>McpServer</PackageType>
    <PackageId>Example.Orders.Mcp</PackageId>
    <Version>1.0.0</Version>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <Description>Read and cancel orders from an MCP client.</Description>
  </PropertyGroup>
  <ItemGroup>
    <None Include=".mcp/server.json" Pack="true" PackagePath="/.mcp/" />
    <None Include="README.md" Pack="true" PackagePath="/" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
    <PackageReference Include="ModelContextProtocol" Version="2.4.0" />
  </ItemGroup>
</Project>
```

- `<PackageType>McpServer</PackageType>` makes nuget.org list the package as an MCP server.
- The template also sets `RuntimeIdentifiers`, `SelfContained`, and `PublishSingleFile`. Then `dotnet pack` makes one package for each runtime identifier and one tool package. Push all of them. Keep these settings when the clients have no .NET runtime.
- In a repo with Central Package Management, put the versions in `Directory.Packages.props`. Use the versions of the repo for `Microsoft.Extensions.Hosting`. Without Central Package Management, use its latest 10.0.x patch, not 10.0.0.

## server.json

```json
{
  "$schema": "https://static.modelcontextprotocol.io/schemas/2025-12-11/server.schema.json",
  "name": "io.github.example/orders-mcp",
  "description": "Read and cancel orders.",
  "version": "1.0.0",
  "packages": [
    {
      "registryType": "nuget",
      "identifier": "Example.Orders.Mcp",
      "version": "1.0.0",
      "transport": { "type": "stdio" },
      "environmentVariables": [
        { "name": "ORDERS_API_URL", "description": "Base URL of the order API.", "isRequired": true }
      ]
    }
  ],
  "repository": { "url": "https://github.com/example/orders-mcp", "source": "github" }
}
```

- nuget.org reads only the first `packages` entry with `registryType: "nuget"`.
- Declare each input in `environmentVariables` or `packageArguments`. Mark a secret with `"isSecret": true`. Never put a secret value in the file.
- Keep `version` the same as the package version. Change both in one commit.
- The MCP Registry: publish with `mcp-publisher` only when the human wants a public listing. Add the line `mcp-name: io.github.example/orders-mcp` to the package README. The registry uses it to check that you own the package.

## Pack, publish, and start with dnx

```
dotnet pack -c Release
dotnet nuget push "bin/Release/*.nupkg" --source <feed>
```

A client starts the server with this command:

```
dnx Example.Orders.Mcp@1.0.0 --yes
```

`dnx` comes with the .NET 10 SDK. Pin the version in the client configuration. Without a version, each start can get a new release.

## Tests with an in-memory client

Test each tool through the real protocol, without a process or a port. The server and the client talk over two pipes. This test ran with `ModelContextProtocol` 2.4.0:

```csharp
[TestMethod]
public async Task GetOrderStatus_KnownOrder_ReturnsStructuredStatus()
{
    var clientToServer = new Pipe();
    var serverToClient = new Pipe();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IOrderStore>(new FakeOrderStore(new OrderStatus("1", "open", null)));
    services.AddMcpServer()
        .WithStreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream())
        .WithTools<OrderTools>();
    await using var provider = services.BuildServiceProvider();
    using var stop = new CancellationTokenSource();
    var run = provider.GetRequiredService<McpServer>().RunAsync(stop.Token);

    await using var client = await McpClient.CreateAsync(
        new StreamClientTransport(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream()));
    var tools = await client.ListToolsAsync();
    var result = await client.CallToolAsync("get_order_status",
        new Dictionary<string, object?> { ["orderId"] = "1" });

    Assert.IsTrue(tools.Single(t => t.Name == "get_order_status").ProtocolTool.Annotations?.ReadOnlyHint);
    Assert.AreNotEqual(true, result.IsError);
    stop.Cancel();
}
```

Test at least these cases:

- `tools/list`: the name, the annotations, and the input schema of each tool.
- Each tool: one success case and one `McpException` case (`result.IsError` is `true`).
- Each destructive tool: an `ElicitationHandler` in `McpClientOptions.Handlers` that returns `accept`, and one that returns `decline`.
- Each input limit: a value over the maximum gives an error.
- Each structured result with a `null` value: the property is in `StructuredContent`.

`WithTools<T>()` keeps a disposed service provider alive (SDK issue 1898). In a test class that builds many hosts, watch the memory.

For an HTTP server, add tests with `WebApplicationFactory` (`dev-stack:dotnet-testing`). A call without a token gets 401 with `resource_metadata`. A token with a wrong audience gets 401.

## MCP Inspector

```
npx @modelcontextprotocol/inspector
```

The Inspector lists the tools, prompts, and resources, calls them, and runs the OAuth flow of an HTTP server. Use it for a manual check before a release. It does not replace the tests.
