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
| 8 | No `.WithOpenApi(...)`. | tool (ASP-WITH-OPENAPI, ASPDEPR002) |
| 9 | XML comments are on `internal` or `public` named handler methods and records, not on lambdas or `private` methods. `GenerateDocumentationFile` is on, with CS1591 in `NoWarn` of that project. | judge |
| 10 | The schema of numbers and enums matches decision API-11, and a change of the JSON options in an existing API is a decision. | judge |
| 11 | Responses are records, not EF entities. | judge |

## Transformers

| # | Check | Type |
|---|-------|------|
| 12 | Transformers use `using Microsoft.OpenApi;` and the 2.x types. | tool (OAS-OPENAPI-MODELS-NS, CS0234) |
| 13 | Nullable types use `JsonSchemaType.Null` flags, examples use `Examples`. | tool (CS0618) and judge |
| 14 | A transformer that adds a security scheme initializes `Components` and `SecuritySchemes`. | judge |

## Build-time document and CI

| # | Check | Type |
|---|-------|------|
| 15 | `OpenApiDocumentsDirectory` points to a tracked folder outside `bin/` and `obj/`. | judge |
| 16 | The committed document matches the build (`git diff --exit-code`). | tool (CI) |
| 17 | Startup work that needs a database or a secret is skipped when the entry assembly is `GetDocument.Insider`. | judge |
| 18 | oasdiff runs for an API with an external consumer (decision API-19). | judge |

## Versioning

| # | Check | Type |
|---|-------|------|
| 19 | Packages are `Asp.Versioning.*`, not `Microsoft.AspNetCore.Mvc.Versioning*`. | tool (OAS-PKG-VERSIONING-OLD, OAS-VERSIONING-OLD) |
| 20 | One `AddOpenApi()` on the versioning builder and `MapOpenApi().WithDocumentPerVersion()`, not one document per version by hand. | tool (AV0029, AV0030) |
| 21 | The URL segment reader has `SubstituteApiVersionInUrl = true` and no `AssumeDefaultVersionWhenUnspecified`. | tool (OAS-URL-ASSUME-DEFAULT) and judge |
| 22 | `ReportApiVersions` is on. | judge |
| 23 | A deprecated version has a sunset date in the future (API-18) and a `text/html` policy link. | judge |
| 24 | Each version has its own request and response records. | judge |
| 25 | A removed or renamed field, a new required field, a changed type, or a changed status code is in a new major version. | judge (oasdiff helps) |

## Tests

| # | Check | Type |
|---|-------|------|
| 26 | A test reads `/openapi/v1.json` in Development and checks the responses of each new endpoint. | judge |
| 27 | A test checks that Production does not serve the document and the UI. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 28 | API-09, API-10, API-18, and API-19 are accepted in `docs/dev-team/decisions.md`, or the change asks them. | judge |
| 29 | For an external consumer, API-10 and API-11 are answered before the first endpoint. | judge |
