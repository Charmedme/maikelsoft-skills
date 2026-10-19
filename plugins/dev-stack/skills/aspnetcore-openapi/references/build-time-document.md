# The build-time document

How the build writes the OpenAPI document into the repository, and how CI finds a contract change. Tested with SDK 10.0.402 and Microsoft.Extensions.ApiDescription.Server 10.0.13.

## Contents

- Project settings
- File names
- Startup code at build time
- CI: the document diff
- CI: oasdiff
- Lint (optional)

## Project settings

Package: `Microsoft.Extensions.ApiDescription.Server` (approval of the human, stack rule 5). With Central Package Management, put the version in `Directory.Packages.props` and the reference without a version in the project.

```xml
<PropertyGroup>
  <!-- The build writes the OpenAPI document to a tracked folder, so a pull request shows contract changes. -->
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../../docs/openapi</OpenApiDocumentsDirectory>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.ApiDescription.Server">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

- Without `OpenApiDocumentsDirectory`, the file goes to `obj/`. Git ignores `obj/`, so the contract is not in the repository.
- The folder is relative to the project. Use a folder outside `bin/` and `obj/`, for example `docs/openapi` at the root of the repository.
- Each `dotnet build` writes the file again. A test run builds too, so the file is current after each build.

Done when `dotnet build` writes the file into the tracked folder and `git status` shows it.

## File names

To write one document only, or to change the file name, set `<OpenApiGenerateDocumentsOptions>--document-name v2</OpenApiGenerateDocumentsOptions>` or `--file-name orders-api`.

| Document | File |
|---|---|
| `AddOpenApi()` (the document `v1`) | `<Project>.json`, for example `Orders.Api.json` |
| Another document, for example `v2` from `WithDocumentPerVersion` | `<Project>_v2.json` |

## Startup code at build time

The build starts the app to make the document. It needs `dotnet` on the `PATH`. Without it, the build fails with "dotnet: not found".

Tested with SDK 10.0.402: in this mode the whole app runs in the `Production` environment. That includes the code after `builder.Build()` and `StartAsync` of each hosted service. The entry assembly is `GetDocument.Insider`. Code that connects to a database, reads a secret from a vault, or calls a network service makes the build fail or hang. Rules:

- Keep connections lazy. `AddDbContext` and `AddHttpClient` do not connect at registration.
- Skip startup work (migrations, cache warm-up, a vault provider, a message bus connection) when the entry assembly is `GetDocument.Insider`:

```csharp
using System.Reflection;

// The build starts the app to write the OpenAPI document. Skip work that needs a database or a secret.
var isDocumentBuild = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";
if (!isDocumentBuild)
{
    builder.Services.AddHostedService<CacheWarmUpService>();
}
```

- Keep the registrations that the endpoints need (the DbContext, the options, the handlers), because the app must start in this mode.
- `ValidateOnStart` runs in this mode too. Give each required option a value in `appsettings.json` that is valid without secrets, or the build fails.

## CI: the document diff

The document in the repository must match the code. CI builds, then compares:

```bash
dotnet build -warnaserror
git diff --exit-code -- docs/openapi
```

Pass: exit code 0. A diff means that a developer changed the contract and did not commit the new document. The fix is to build and commit the file.

## CI: oasdiff

oasdiff (Apache-2.0) compares two OpenAPI documents and lists breaking changes. Decision API-19 says when it is a gate.

```bash
git show origin/main:docs/openapi/Orders.Api.json > /tmp/base.json
oasdiff breaking --fail-on ERR /tmp/base.json docs/openapi/Orders.Api.json
```

- Pass: exit code 0. With an `ERR` finding, the change breaks clients: make a new major version (`references/versioning.md`), or revert the change.
- For an internal API, run the same command without `--fail-on` and post the output in the pull request.
- Install oasdiff in the CI image as a binary or a container. It is a tool, not a NuGet package, but it still needs the approval of the human (stack rule 5).
- Run it for each document file (`Orders.Api.json`, `Orders.Api_v2.json`).

## Lint (optional)

Spectral (Apache-2.0) checks style rules of the document:

```bash
spectral lint docs/openapi/Orders.Api.json --ruleset spectral:oas
```

Use it when a partner asks for a style guide. It does not find breaking changes.
