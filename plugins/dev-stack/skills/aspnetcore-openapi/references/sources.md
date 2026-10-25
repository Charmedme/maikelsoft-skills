# Sources: aspnetcore-openapi

The author checked all facts on 2026-10-19, unless a line says otherwise. The author checked the versions again on 2026-10-25 on nuget.org and in the .NET release metadata. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Tools
- Checked by compile and run
- Credits

## Versions

| Item | Version on 2026-10-25 | Source |
|------|-----------------------|--------|
| Microsoft.AspNetCore.OpenApi | 10.0.13 (11.0.0-rc.2 is a preview). Depends on Microsoft.OpenApi `[2.12.2, 3.0.0)` | https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi |
| Microsoft.OpenApi | 2.12.2 is the minimum of .NET 10 (`[2.12.2, 3.0.0)`), 2.13.0 is the newest 2.x. 3.11.0 is the newest | https://www.nuget.org/packages/Microsoft.OpenApi |
| Microsoft.Extensions.ApiDescription.Server | 10.0.13 | https://www.nuget.org/packages/Microsoft.Extensions.ApiDescription.Server |
| Asp.Versioning.Http and Asp.Versioning.OpenApi | 10.2.3. `.OpenApi` depends on Microsoft.OpenApi `[2.7.5, 3.0.0)` | https://www.nuget.org/packages/Asp.Versioning.OpenApi |
| Asp.Versioning.Mvc and Asp.Versioning.Mvc.ApiExplorer | 10.2.1 | https://www.nuget.org/packages/Asp.Versioning.Mvc.ApiExplorer |
| Scalar.AspNetCore | 2.18.3 | https://www.nuget.org/packages/Scalar.AspNetCore |
| Swashbuckle.AspNetCore | 10.3.1, depends on Microsoft.OpenApi 2.7.5 or later | https://www.nuget.org/packages/Swashbuckle.AspNetCore |
| NSwag.AspNetCore | 14.7.1 | https://www.nuget.org/packages/NSwag.AspNetCore |
| .NET | 10.0.13 (SDK 10.0.402, released 2026-10-13, LTS to 2028-11-14), 11.0.0-rc.2 | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core |

## Documentation

- OpenAPI in ASP.NET Core: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/overview
- Generate OpenAPI documents (build-time document, `OpenApiDocumentsDirectory`): https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi
- Include OpenAPI metadata (`WithName`, `Produces`, `ProducesProblem`, XML comments): https://learn.microsoft.com/aspnet/core/fundamentals/openapi/include-metadata
- Customize documents with transformers: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/customize-openapi
- Microsoft.OpenApi 2.0 upgrade guide: https://github.com/microsoft/OpenAPI.NET/blob/main/docs/upgrade-guide-2.md
- ASPDEPR002 (`WithOpenApi` deprecated, use `AddOpenApiOperationTransformer`): https://learn.microsoft.com/aspnet/core/breaking-changes/10/withopenapi-deprecated and https://learn.microsoft.com/aspnet/core/diagnostics/aspdepr002
- Build-time generation starts the entry point with a mock server, and the docs show the entry assembly check: https://learn.microsoft.com/aspnet/core/fundamentals/openapi/aspnetcore-openapi#generate-openapi-documents-at-build-time
- ASP.NET Core 11 moves to Microsoft.OpenApi 3.x, and the default document version becomes OpenAPI 3.2: https://learn.microsoft.com/aspnet/core/breaking-changes/11/microsoft-openapi-3x
- Asp.Versioning wiki and examples (`MinimalOpenApiExample`, `OpenApiExample`): https://github.com/dotnet/aspnet-api-versioning
- Asp.Versioning diagnostics AV0029 and AV0030: https://dotnet.github.io/aspnet-api-versioning/diagnostic/av0029.html, https://dotnet.github.io/aspnet-api-versioning/diagnostic/av0030.html
- Microsoft REST API guidelines (breaking changes): https://github.com/microsoft/api-guidelines
- Scalar for ASP.NET Core: https://github.com/scalar/scalar/tree/main/integrations/aspnetcore

## Tools

- oasdiff (Apache-2.0): https://github.com/oasdiff/oasdiff. The command form `oasdiff breaking --fail-on ERR <base> <revision>` is from a public CI workflow, checked on 2026-10-19: https://git.kvant.cloud/phoenix-oss/llama-stack-mirror/src/commit/3fa4e3ff20a01b5de1f7980848786adcf3c802aa/.github/workflows/conformance.yml
- Spectral (Apache-2.0): https://github.com/stoplightio/spectral

## Checked by compile and run

The author compiled and ran these on 2026-10-19 with SDK 10.0.402, in the `orders-api` eval fixture and in small lab projects:

- A `TypedResults.Problem(statusCode: 404)` in a `Results<Ok<T>, ProblemHttpResult>` union is not in the document. With `.ProducesProblem(404)` the document has a 404 with `application/problem+json`.
- An endpoint without `WithName` has no `operationId`.
- The XML comment generator reads comments on `internal` and `public` handler methods. It skips a `private` method (same code, only the access modifier changed) and a lambda.
- With the web defaults, `int` is `["integer","string"]`. With `NumberHandling = Strict` and `JsonStringEnumConverter` it is `integer`, and the enum has names. `decimal` is `number` with `format: double`.
- `MapHealthChecks` endpoints are not in the document.
- `schema.Example` gives CS0618 ("Use Examples instead") with Microsoft.OpenApi 2.12.2. `using Microsoft.OpenApi.Models;` gives CS0234.
- Without `OpenApiDocumentsDirectory`, the build writes the document to `obj/`. With it, to the folder. With `WithDocumentPerVersion`, the files are `<Project>.json` and `<Project>_v2.json`.
- At build time, the document tool runs the app in `Production`: code after `builder.Build()` and `StartAsync` of a hosted service ran. The entry assembly name is `GetDocument.Insider`, with the argument `--applicationName=<Project>`.
- Two hand-written `AddOpenApi("v1")` and `AddOpenApi("v2")` with Asp.Versioning give AV0029 (also in a generated file) and AV0030 as warnings. The setup with `.AddOpenApi()` on the versioning builder and `WithDocumentPerVersion()` gives no warning.
- URL segment without `SubstituteApiVersionInUrl`: the document path is `/api/v{version}/orders/{id}`. With `AssumeDefaultVersionWhenUnspecified = true`, `/api/orders/{id}` gives 404.
- From the concept lab of 2026-10-19: in Production, `MapOpenApi` and Scalar return 404 when they are inside the environment check. A deprecated version with a sunset policy sends `api-deprecated-versions`, `Sunset`, and `Link` headers.

### Checked on 2026-10-25

SDK 10.0.402, Microsoft.AspNetCore.OpenApi 10.0.13, a small lab project, `/openapi/v1.json` read at run time:

- `.WithOpenApi(op => { op.Summary = ...; op.Description = ...; return op; })` compiles (with ASPDEPR002) and runs, but the summary and the description are not in the document. `WithSummary` and `AddOpenApiOperationTransformer` on the same kind of endpoint are in the document. Microsoft Learn says only that the call "still compiles and executes": https://learn.microsoft.com/aspnet/core/breaking-changes/10/withopenapi-deprecated
- An `IResult` handler with `.Produces(200, contentType: "application/pdf")` gives a 200 with no content. A handler that returns `TypedResults.File(...)` gives the same. `Results<FileContentHttpResult, NotFound>` gives only the 404.
- `.Produces<Stream>(200, "application/pdf")` and `.Produces<IFormFile>(...)` give `type: string, format: binary`. `.Produces<byte[]>(...)` gives `format: byte`. `.Produces(200, typeof(FileContentHttpResult), ...)` gives an object schema with the properties of the result class.

## Credits

- github.com/dotnet/skills, skill `dotnet-webapi` (MIT, .NET team): built-in OpenAPI for new projects, `TypedResults` for metadata, enums as strings. Paraphrased. https://github.com/dotnet/skills
- github.com/dotnet/aspnet-api-versioning examples `MinimalOpenApiExample` and `OpenApiExample` (MIT): the setup chain with `AddApiExplorer().AddOpenApi()` and `WithDocumentPerVersion()`. Paraphrased.
- Microsoft REST API guidelines (code MIT, text CC BY 4.0): the list of breaking changes, ideas only.
