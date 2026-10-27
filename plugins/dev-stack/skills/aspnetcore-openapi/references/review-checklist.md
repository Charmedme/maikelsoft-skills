# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Exposure
- Document content
- Transformers
- Build-time document and CI
- Versioning
- Tests
- Decisions

## Exposure

| # | Check | Type |
|---|-------|------|
| 1 | `MapOpenApi`, `MapScalarApiReference`, `UseSwagger`, and `UseSwaggerUI` are inside `if (app.Environment.IsDevelopment())`, or the document has `RequireAuthorization`. | tool (OAS-UI-UNGUARDED) |
| 2 | No UI is mapped in Production. | judge |
| 3 | No Swashbuckle in a new .NET 10 API. An existing Swashbuckle setup stays. | judge |

## Document content

| # | Check | Type |
|---|-------|------|
| 4 | Each endpoint returns a `Results<...>` union, or declares its responses with `Produces...`. | tool (ASP-RESULTS-UNTYPED in `dev-stack:aspnetcore`) and judge |
| 5 | A `TypedResults.Problem(...)` status has `.ProducesProblem(status)`. | judge |
| 6 | An endpoint with `AddValidation` or a validation filter has `.ProducesValidationProblem()`, unless its union has `ValidationProblem`. | judge |
| 7 | Each endpoint has a unique `.WithName(...)`, also the existing endpoints. | judge (a document test) |
| 8 | No `.WithOpenApi(...)`. Its delegate does not reach the document, and a `NoWarn` for ASPDEPR002 hides the call. | tool (ASP-WITH-OPENAPI, ASPDEPR002) and judge |
| 9 | XML comments are on `internal` or `public` named handler methods and records, not on lambdas or `private` methods. `GenerateDocumentationFile` is on, with CS1591 in `NoWarn` of that project. | judge |
| 10 | The schema of numbers and enums matches decision API-11, and a change of the JSON options in an existing API is a decision. | judge |
| 11 | Responses are records, not EF entities. | judge |
| 12 | A file response has `.Produces<Stream>(200, "<content type>")`, so the document has a binary schema. | judge |
| 13 | Each documented status and each `Required` header or parameter agrees with the handler and its filters. | judge |

## Transformers

| # | Check | Type |
|---|-------|------|
| 14 | Transformers use `using Microsoft.OpenApi;` and the 2.x types. | tool (OAS-OPENAPI-MODELS-NS, CS0234) |
| 15 | Nullable types use `JsonSchemaType.Null` flags, examples use `Examples`. | tool (CS0618) and judge |
| 16 | A transformer that adds a security scheme initializes `Components` and `SecuritySchemes`. | judge |

## Build-time document and CI

| # | Check | Type |
|---|-------|------|
| 17 | `OpenApiDocumentsDirectory` points to a tracked folder outside `bin/` and `obj/`. | judge |
| 18 | The committed document matches the build (`git diff --exit-code`). | tool (CI) |
| 19 | Startup work that needs a database or a secret is skipped when the entry assembly is `GetDocument.Insider`. | judge |
| 20 | oasdiff runs for an API with an external consumer (decision API-19). | judge |

## Versioning

| # | Check | Type |
|---|-------|------|
| 21 | Packages are `Asp.Versioning.*`, not `Microsoft.AspNetCore.Mvc.Versioning*`. | tool (OAS-PKG-VERSIONING-OLD, OAS-VERSIONING-OLD) |
| 22 | One `AddOpenApi()` on the versioning builder and `MapOpenApi().WithDocumentPerVersion()`, not one document per version by hand. | tool (AV0029, AV0030) |
| 23 | The URL segment reader has `SubstituteApiVersionInUrl = true` and no `AssumeDefaultVersionWhenUnspecified`. | tool (OAS-URL-ASSUME-DEFAULT) and judge |
| 24 | `ReportApiVersions` is on. | judge |
| 25 | A deprecated version has a deprecation policy, a sunset date in the future (API-18), and a `text/html` policy link. | judge |
| 26 | Each version has its own request and response records. | judge |
| 27 | A removed or renamed field, a new required field, a changed type, or a changed status code is in a new major version. | judge (oasdiff helps) |

## Tests

| # | Check | Type |
|---|-------|------|
| 28 | A test reads `/openapi/v1.json` in Development and checks the responses of each new endpoint. | judge |
| 29 | A test checks that Production does not serve the document and the UI. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 30 | API-09, API-10, API-18, and API-19 are accepted in `docs/dev-team/decisions.md`, or the change asks them. | judge |
| 31 | For an external consumer, API-10 and API-11 are answered before the first endpoint. | judge |
