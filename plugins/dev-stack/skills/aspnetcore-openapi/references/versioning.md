# API versions with Asp.Versioning

Setup, the URL segment, deprecation and sunset, contracts per version, and the breaking-change list. The example compiled with SDK 10.0.402, Asp.Versioning.Http and Asp.Versioning.OpenApi 10.2.3, and `-warnaserror`. It made one build-time document per version.

## Contents

- Packages
- Setup
- URL segment rules
- Deprecation and sunset
- Contracts per version
- Breaking changes
- Old packages and APIs
- .NET 11

## Packages

| Package | Use |
|---|---|
| `Asp.Versioning.Http` | Minimal APIs: `AddApiVersioning`, `NewVersionedApi` |
| `Asp.Versioning.OpenApi` | `.AddOpenApi()` on the versioning builder and `WithDocumentPerVersion()`. It brings `Asp.Versioning.Mvc.ApiExplorer` |
| `Asp.Versioning.Mvc` and `Asp.Versioning.Mvc.ApiExplorer` | Controllers |

`Microsoft.AspNetCore.Mvc.Versioning` and `Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer` are the old names. Do not add them.

## Setup

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.Policies.Deprecate(1.0)
            .Effective(2026, 12, 31)
            .Link("https://developer.example.com/orders/versioning")
            .Title("Versioning policy")
            .Type("text/html");
        options.Policies.Sunset(1.0)
            .Effective(2027, 6, 30)
            .Link("https://developer.example.com/orders/versioning")
            .Title("Versioning policy")
            .Type("text/html");
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

var orders = app.NewVersionedApi("Orders");

var v1 = orders.MapGroup("/api/v{version:apiVersion}/orders").HasDeprecatedApiVersion(1.0);
v1.MapGet("/{id:guid}", OrderEndpointsV1.GetAsync).WithName("GetOrderV1");

var v2 = orders.MapGroup("/api/v{version:apiVersion}/orders").HasApiVersion(2.0);
v2.MapGet("/{id:guid}", OrderEndpointsV2.GetAsync)
    .WithName("GetOrderV2")
    .ProducesProblem(StatusCodes.Status404NotFound);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.AddDocument(description.GroupName);
        }
    });
}

await app.RunAsync();
```

The result: `/openapi/v1.json` with `/api/v1/orders/{id}` (marked `deprecated`), and `/openapi/v2.json` with `/api/v2/orders/{id}`. With the build-time document, the files are `<Project>.json` and `<Project>_v2.json`.

Each version has its own handler class and its own records:

```csharp
internal static class OrderEndpointsV2
{
    /// <summary>Gets one order.</summary>
    /// <param name="id">The order id.</param>
    internal static Results<Ok<OrderResponseV2>, ProblemHttpResult> GetAsync(Guid id) =>
        id == Guid.Empty
            ? TypedResults.Problem(statusCode: StatusCodes.Status404NotFound, title: "The order does not exist.")
            : TypedResults.Ok(new OrderResponseV2(id, DateTimeOffset.UnixEpoch));
}

/// <summary>An order in version 2.</summary>
/// <param name="Id">The order id.</param>
/// <param name="CreatedAt">When the customer placed the order.</param>
public sealed record OrderResponseV2(Guid Id, DateTimeOffset CreatedAt);
```

The hand-written form `AddOpenApi("v1", o => o.ShouldInclude = ...)` per version builds, but the analyzers warn:

- AV0029: "AddApiVersioning().AddOpenApi() registers the OpenAPI services that describe API versions".
- AV0030: "Call MapOpenApi().WithDocumentPerVersion()".

With this form, the analyzer also reports AV0029 in a generated file of the XML comment generator. Replace the form. Do not suppress the warning.

## URL segment rules

- `SubstituteApiVersionInUrl = true` puts the version into each path of the document. Without it, the document shows `/api/v{version}/orders/{id}` and a required `version` parameter, and generated clients need a version argument for each call.
- `AssumeDefaultVersionWhenUnspecified` has no effect with the URL segment reader. `/api/orders/{id}` gives 404, not v1. When old clients call a path without a version, keep that route as a separate unversioned group on purpose, and document it.
- An unknown version (`/api/v3/orders/{id}`) gives 404, not 400.
- Use the URL segment for an external API (decision API-10). A query string or a header is harder to cache, to route in a gateway, and to see in logs.

## Deprecation and sunset

1. Mark the old version: `HasDeprecatedApiVersion(1.0)` on its group. The document marks its operations `deprecated: true`.
2. Turn on `ReportApiVersions`. Responses then carry `api-supported-versions` and `api-deprecated-versions`.
3. Add a deprecation policy: `Policies.Deprecate(1.0).Effective(...)` with a `text/html` link. Responses of that version then carry the `Deprecation` header (RFC 9745, `@<unix time>`) and a `Link` with `rel="deprecation"`.
4. Add a sunset policy with a date in the future and a `text/html` link to the versioning policy. Responses then carry the `Sunset` header and a `Link` with `rel="sunset"`. The date is at least 6 months after the deprecation, 12 months for partners (decision API-18).
5. Tell the consumers. The header alone is not a notice.
6. Remove the version after the sunset date, in a release of its own.

A sunset date in the past gives the clients no window. In a review, it is a finding.

`Effective(year, month, day)` uses the local time zone of the server (tested: `Effective(2027, 6, 30)` on a server in UTC+2 sent `Sunset: Tue, 29 Jun 2027 22:00:00 GMT`). For an exact UTC date, pass a `DateTimeOffset` with offset zero.

## Contracts per version

- Each version has its own request and response records (`OrderResponseV1`, `OrderResponseV2`). A shared record changes each version when one version changes.
- Do not return EF entities. A column change then changes the contract.
- Put the records of a version in their own folder or namespace (`Orders/V1`, `Orders/V2`). Then a pull request shows the version that a change touches.
- Map from the domain to each version with its own `ToResponseV1()` and `ToResponseV2()` methods.

## Breaking changes

A change that breaks an existing client needs a new major version. The list follows the Microsoft REST API guidelines:

| Change | Breaking |
|---|---|
| Remove or rename a field, an endpoint, a query parameter, or an enum value | Yes |
| Add a required request field or parameter | Yes |
| Change the type or the format of a field | Yes |
| Change a status code, or the media type of a response | Yes |
| Add an optional request field, a response field, or an endpoint | No |
| Add an enum value to a response | Often yes for generated clients. Ask the consumer |

oasdiff (`references/build-time-document.md`) finds most of these changes. A judge still checks the meaning of a field: the same name with a new meaning is breaking, and no tool sees it.

## Old packages and APIs

| Old | Current |
|---|---|
| `services.AddVersionedApiExplorer(...)` | `AddApiVersioning(...).AddApiExplorer(...)` |
| `using Microsoft.AspNetCore.Mvc.Versioning;` | `using Asp.Versioning;` |
| `apiDescription.IsDeprecated()` | `apiDescription.IsDeprecated` (extension property, `using Microsoft.AspNetCore.Mvc.ApiExplorer;`) |
| Swashbuckle `SwaggerDoc` per version with `IConfigureOptions<SwaggerGenOptions>` | The built-in generator with `WithDocumentPerVersion()` in a new API. Keep the Swashbuckle setup in an existing API |

## .NET 11

ASP.NET Core 11 moves `Microsoft.AspNetCore.OpenApi` to Microsoft.OpenApi 3.x. `Asp.Versioning.OpenApi` 10.2.3 depends on Microsoft.OpenApi `[2.7.5, 3.0.0)`. A project with this package stays on .NET 10 until a new Asp.Versioning release supports 3.x. Look up the current release before an upgrade (stack rule 2). On 2026-10-27, Asp.Versioning 10.2.3 is still the newest release.
