---
name: aspnetcore-openapi
description: OpenAPI documents and API versions for ASP.NET Core 10 APIs. Covers the built-in generator (Microsoft.AspNetCore.OpenApi), response metadata for client generators, Scalar and Swagger UI, transformers on Microsoft.OpenApi 2.x, the build-time document in the repository, versioning with Asp.Versioning (URL segment, deprecation, sunset), and breaking-change checks with oasdiff. Use when code or a review touches AddOpenApi, MapOpenApi, MapScalarApiReference, Swashbuckle, Produces or ProducesProblem, WithName or operationId, a document, operation or schema transformer, Microsoft.Extensions.ApiDescription.Server, OpenApiDocumentsDirectory, AddApiVersioning, NewVersionedApi, a deprecated API version, or a contract that an external client generates code from. Not for endpoint design, ProblemDetails wiring, or JSON options (dev-stack:aspnetcore), client generation with Kiota or NSwag, or validation rules (dev-stack:fluentvalidation).
metadata:
  verified: "2026-10-19"
  versions: ".NET 10 (SDK 10.0.402), Microsoft.AspNetCore.OpenApi 10.0.13, Microsoft.OpenApi 2.12.2, Microsoft.Extensions.ApiDescription.Server 10.0.13, Asp.Versioning 10.2.3, Scalar.AspNetCore 2.18.0, Swashbuckle.AspNetCore 10.3.0"
---

# ASP.NET Core OpenAPI and versioning

The OpenAPI document, the metadata that feeds it, the UI, the build-time document, and API versions. Endpoints, ProblemDetails, and JSON options: `dev-stack:aspnetcore`. This skill does not repeat those rules.

## First

1. Read `references/stack-rules.md`.
2. Read the version gate:
   - `TargetFramework` and the packages in `*.csproj` and `Directory.Packages.props`.
   - `net10.0` with `Microsoft.AspNetCore.OpenApi` 10.x: use this file. The document is OpenAPI 3.1, and transformers use Microsoft.OpenApi 2.x.
   - `Swashbuckle.AspNetCore` in the project: keep it. Swashbuckle 10 also uses Microsoft.OpenApi 2.x, so rule 7 applies. Do not migrate unless the task asks for it.
   - `net11.0`: ASP.NET Core 11 moves to Microsoft.OpenApi 3.x. `Asp.Versioning.OpenApi` 10.2.3 allows only Microsoft.OpenApi below 3.0. An API with versioning stays on .NET 10 until a new Asp.Versioning release supports 3.x. Look up the current state (stack rule 2).
3. Find who reads the document: only the team, or an external client that generates code. An external consumer needs decisions API-10 and API-11 before the first endpoint.

Done when you can name the generator, the UI, the consumer of the document, and the versioning scheme (or "none").

## Defaults for new code

Existing code keeps its style. For new code, use the accepted decision in `docs/dev-team/decisions.md`. When a decision has no accepted answer, ask the human (stack rule 4). The detail is in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| Generator and UI | `Microsoft.AspNetCore.OpenApi` and Scalar, both mapped in Development only | API-09 |
| Contract in the repository | Build-time document in a tracked folder, committed with each contract change | API-09 |
| Versioning | None for one internal consumer. URL segment with Asp.Versioning for an external or multi-consumer API, from the first endpoint | API-10 |
| Version format and window | Major versions (`v1`), 6 months after deprecation, 12 months for partners | API-18 |
| Breaking-change gate | oasdiff in CI: a gate on `ERR` for an external consumer, a report for internal APIs | API-19 |
| Schema of numbers and enums | Follows the JSON options of decision API-11 (`dev-stack:aspnetcore`) | API-11 |
| Error responses | ProblemDetails (API-04), documented with `ProducesProblem` and `ProducesValidationProblem` | API-04 |

## Rules

### Document and UI

1. Use `builder.Services.AddOpenApi()` and `app.MapOpenApi()` in a new .NET 10 API. Keep Swashbuckle 10 where it exists. The built-in generator writes OpenAPI 3.1 and works with `Asp.Versioning.OpenApi`.
2. Map the document and the UI inside `if (app.Environment.IsDevelopment())`. When a public document is a decision, add `.RequireAuthorization("<policy>")`. The document shows each route to an attacker. Give an external partner the build-time file instead.
3. Declare each status code that a client can get. A `TypedResults.Problem(...)` inside a `Results<...>` union does not document its status: add `.ProducesProblem(StatusCodes.Status404NotFound)`. The automatic 400 of `AddValidation` and a 400 from an endpoint filter are not in the document either: add `.ProducesValidationProblem()`. A typed result in the union (`NotFound`, `ValidationProblem`) documents itself. A client generator knows only what the document shows.
4. Give each endpoint a unique `.WithName("GetOrder")`, also the existing ones. It becomes the `operationId`, and generators name their methods after it. An endpoint without a name has no `operationId`.
5. Put XML comments on named handler methods that are `internal` or `public`, and on the contract records. Set `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `<NoWarn>$(NoWarn);CS1591</NoWarn>` in that project. The source generator skips comments on lambdas and on `private` methods (tested). Without the `NoWarn`, CS1591 warns for each public member without a comment.
6. The schema follows the JSON options. With the web defaults an `int` is `"type": ["integer","string"]` with a pattern, and an enum is an integer. Many generators cannot read the first form. Decision API-11 (`NumberHandling = Strict`, `JsonStringEnumConverter`) gives `integer` and named enum values. Ask before you change the JSON options of an existing API, because the change breaks clients.
7. Write transformers for Microsoft.OpenApi 2.x: `using Microsoft.OpenApi;`, `JsonSchemaType` flags, `Examples` as a list of `JsonNode`, and initialize `Components.SecuritySchemes` with `IOpenApiSecurityScheme` values. The namespaces `Microsoft.OpenApi.Models` and `Microsoft.OpenApi.Any`, `OpenApiString`, and `schema.Nullable` do not exist in 2.x (CS0234). `schema.Example` is obsolete (CS0618).
8. Change one operation with `.WithSummary(...)`, `.WithDescription(...)`, or `.AddOpenApiOperationTransformer(...)`. Microsoft deprecated `.WithOpenApi(...)` in .NET 10 (ASPDEPR002).

Transformer and metadata examples: `references/document.md`.

### Build-time document

9. Add `Microsoft.Extensions.ApiDescription.Server` and set `<OpenApiDocumentsDirectory>` to a tracked folder (for example `$(MSBuildProjectDirectory)/../../docs/openapi`). By default the build writes the file to `obj/`, which git ignores. The file names are `<Project>.json` for the document `v1` and `<Project>_<name>.json` for each other document.
10. The build runs the whole app to make the document (tested). This includes the code after `Build()` and `StartAsync` of each hosted service, in the `Production` environment. It needs `dotnet` on the `PATH`. Skip work that needs a database, a secret, or a network call when the entry assembly is `GetDocument.Insider`.
11. Commit the document with each contract change. CI builds and runs `git diff --exit-code` on the folder, then oasdiff against the document on `main` (decision API-19).

Commands, CI steps, and the startup guard: `references/build-time-document.md`.

### Versioning

12. Use `AddApiVersioning(...).AddApiExplorer(...).AddOpenApi()`, `app.NewVersionedApi()`, and `app.MapOpenApi().WithDocumentPerVersion()`. Do not write one `AddOpenApi("v1")` per version with `ShouldInclude`. The analyzers AV0029 and AV0030 warn, and each new version needs new code.
13. URL segment: set `ApiVersionReader = new UrlSegmentApiVersionReader()` and `SubstituteApiVersionInUrl = true`. Without the substitution, the document shows `/api/v{version}/orders` with a version parameter. `AssumeDefaultVersionWhenUnspecified` has no effect for a URL segment: a missing or unknown version gives 404, not 400.
14. Turn on `ReportApiVersions`. Deprecate before you remove: `HasDeprecatedApiVersion(1.0)` and `Policies.Sunset(1.0).Effective(...)`. Give a date in the future (API-18) and a `text/html` link to the policy. Clients then get `api-deprecated-versions`, `Sunset`, and `Link` headers.
15. Give each version its own request and response records. Do not share one record between versions, and do not expose EF entities. A change in a shared type changes each version.
16. A breaking change needs a new major version. Examples: a removed or renamed field, endpoint, or enum value. A new required request field. A changed type, format, or status code. Add a response field or an endpoint in the same version.

Full setup, deprecation, and the breaking-change list: `references/versioning.md`.

## Never write

| Wrong | Correct |
|-------|---------|
| `AddEndpointsApiExplorer(); AddSwaggerGen();` in a new .NET 10 API | `builder.Services.AddOpenApi();` and `app.MapOpenApi();` in Development |
| `app.MapOpenApi(); app.MapScalarApiReference();` at the top level of `Program.cs` | Inside `if (app.Environment.IsDevelopment())`, or `.RequireAuthorization("ApiDocs")` |
| `.WithOpenApi(op => { op.Summary = "x"; return op; })` | `.WithSummary("x")` or `.AddOpenApiOperationTransformer(...)` |
| `Results<Ok<T>, ProblemHttpResult>` with no other metadata | The same plus `.ProducesProblem(StatusCodes.Status404NotFound)` |
| `<response code="404">` in an XML comment as the only metadata | `.ProducesProblem(404)`, or `NotFound` in the `Results<...>` union |
| `/// <summary>` on a lambda or a `private static` handler | An `internal` or `public` named handler method with the comment |
| `using Microsoft.OpenApi.Models;`, `new OpenApiString("x")` | `using Microsoft.OpenApi;`, `JsonValue.Create("x")` |
| `schema.Type = "string"; schema.Nullable = true;` | `schema.Type = JsonSchemaType.String \| JsonSchemaType.Null;` |
| `schema.Example = JsonValue.Create("SKU-1");` | `schema.Examples = [JsonValue.Create("SKU-1")];` |
| `services.AddVersionedApiExplorer(...)`, package `Microsoft.AspNetCore.Mvc.Versioning` | `AddApiVersioning(...).AddApiExplorer(...)`, package `Asp.Versioning.Http` |
| `AddOpenApi("v1"); AddOpenApi("v2");` with `ShouldInclude` | `.AddOpenApi()` after `AddApiExplorer`, and `MapOpenApi().WithDocumentPerVersion()` |
| `AssumeDefaultVersionWhenUnspecified = true` with the URL segment reader | A version in each URL, or a separate unversioned route that you keep on purpose |
| `apiDescription.IsDeprecated()` | `apiDescription.IsDeprecated` (an extension property in `Microsoft.AspNetCore.Mvc.ApiExplorer`) |
| `Effective(2026, 3, 31)` for a version that clients still use | A date at least 6 months after the deprecation (API-18) |

## Check

1. Build with the analyzers. Pass: 0 warnings and 0 errors. ASPDEPR002 (`WithOpenApi`), AV0029 and AV0030 (hand-written versioned documents), and CS0618 show here.

   ```
   dotnet build -warnaserror
   ```

2. Check that the committed document is current. Pass: no diff, or the diff is part of the change.

   ```
   dotnet build && git diff --exit-code -- docs/openapi
   ```

3. Find breaking changes against `main` (decision API-19). Pass: exit code 0, or a new major version.

   ```
   git show origin/main:docs/openapi/Orders.Api.json > /tmp/base.json
   oasdiff breaking --fail-on ERR /tmp/base.json docs/openapi/Orders.Api.json
   ```

4. Run the rule check of this skill. Pass: `stackcheck: 0 errors`. In an existing project, add `--changed <base branch>`.

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

5. Run the tests. Pass: all green. One test reads `/openapi/v1.json` in Development and asserts the responses of each new endpoint. One test shows that Production does not serve the document.

## Review checklist

The full list is in `references/review-checklist.md`.

1. `Program.cs` maps the document and the UI in Development only, or behind a policy. tool (OAS-UI-UNGUARDED)
2. No Swashbuckle in a new API, no `WithOpenApi`. tool (ASP-WITH-OPENAPI, ASPDEPR002) and judge
3. Each status code of each endpoint is in the document, also a `TypedResults.Problem` and the validation 400. judge
4. Each endpoint has a unique `WithName`. judge (a document test)
5. XML comments are on `internal` or `public` named methods, with `GenerateDocumentationFile`. judge
6. Transformers use Microsoft.OpenApi 2.x types. tool (OAS-OPENAPI-MODELS-NS, CS0234, CS0618)
7. The schema of numbers and enums matches decision API-11. judge
8. The build-time document is in a tracked folder and committed. judge
9. Startup code does not need a database or a secret when the build makes the document. judge
10. Versioning uses `AddApiExplorer().AddOpenApi()` and `WithDocumentPerVersion`. tool (AV0029, AV0030, OAS-VERSIONING-OLD)
11. The URL segment reader has `SubstituteApiVersionInUrl` and no `AssumeDefaultVersionWhenUnspecified`. tool (OAS-URL-ASSUME-DEFAULT) and judge
12. A deprecated version has a sunset date in the future and a policy link. judge
13. Each version has its own contract records. A rename or removal is in a new major version. judge
14. CI checks the document diff and runs oasdiff for an external consumer. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`). Output text follows `references/language.md`.

## References

- `references/document.md`: read when you set up the generator or the UI, add response metadata, write a transformer, or change the schema.
- `references/build-time-document.md`: read when the document must be in the repository or in CI, or when the build fails while it makes the document.
- `references/versioning.md`: read before you add a version, deprecate a version, or change a contract that clients use.
- `references/decisions.md`: read when new code needs decision API-09, API-10, API-18, or API-19.
- `references/review-checklist.md`: read when you review a change.
- `references/sources.md`: the primary sources and the date of each check.
- `references/stack-rules.md`: the common rules of each dev-stack skill.
- `references/language.md`: the rules for text output.
- `scripts/stackcheck.py`: the rule check of this skill.
