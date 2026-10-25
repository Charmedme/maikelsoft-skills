# The OpenAPI document

Setup, response metadata, XML comments, transformers, and the schema. Each C# example compiled with SDK 10.0.402, Microsoft.AspNetCore.OpenApi 10.0.13, and `AnalysisLevel` `latest-recommended`.

## Contents

- Setup
- Response metadata
- XML comments
- Transformers
- Schema of numbers and enums
- Swashbuckle in an existing project
- A test for the document

## Setup

Packages: `Microsoft.AspNetCore.OpenApi` and `Scalar.AspNetCore`. Each new package needs the approval of the human (stack rule 5).

```csharp
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json
    app.MapScalarApiReference();    // /scalar
}

app.MapOrders();
await app.RunAsync();
```

- `AddOpenApi()` makes the document `v1`. `AddOpenApi("internal")` makes a second document with its own name.
- When the document must be reachable outside Development, protect it: `app.MapOpenApi().RequireAuthorization("ApiDocs")` with a named policy. Do not map Scalar or Swagger UI in Production.
- Health endpoints (`MapHealthChecks`) do not show in the document.

## Response metadata

The generator reads the return type and the metadata of the endpoint. It does not read the body of the handler.

| The handler returns | In the document | Add |
|---|---|---|
| `Results<Ok<T>, NotFound>` | 200 and 404 | Nothing |
| `Results<Created<T>, ValidationProblem>` | 201 and 400 (`application/problem+json`) | Nothing |
| `Results<Ok<T>, ProblemHttpResult>` with `TypedResults.Problem(statusCode: 404)` | 200 only | `.ProducesProblem(StatusCodes.Status404NotFound)` |
| Any result, with `AddValidation` or a validation filter | No 400 | `.ProducesValidationProblem()` |
| `IResult` | Nothing useful | A `Results<...>` union (`dev-stack:aspnetcore`) |
| `IResult` file with `.Produces(200, contentType: "application/pdf")` | 200 with no content | `.Produces<Stream>(200, "application/pdf")` |
| `TypedResults.File(...)` (`FileContentHttpResult`) | 200 with no content | `.Produces<Stream>(200, "application/pdf")` |
| `Results<FileContentHttpResult, NotFound>` | 404 only, no 200 | `.Produces<Stream>(200, "application/pdf")` |

Tested with Microsoft.AspNetCore.OpenApi 10.0.13 on 2026-10-25. `Produces<Stream>` and `Produces<IFormFile>` give `type: string, format: binary`. Do not use `Produces<byte[]>`: it gives `format: byte` (a base64 string), so a generator expects text. `Produces(200, typeof(FileContentHttpResult), ...)` gives the properties of the result class as an object schema.

The generator reads metadata, not the handler body. So a wrong status or a wrong `Required` value stays in the document without a warning. Compare the metadata with the handler and its filters. Find each status that the code returns (`Problems.X`, `TypedResults.X`). Find each header or parameter that a filter rejects when it is missing.

```csharp
group.MapGet("/{id:guid}", GetAsync)
    .WithName("GetOrder")
    .ProducesProblem(StatusCodes.Status404NotFound);

group.MapPost("/", CreateAsync)
    .WithName("CreateOrder")
    .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
    .ProducesValidationProblem();
```

```csharp
/// <summary>Gets one order with its lines.</summary>
/// <param name="id">The order id.</param>
/// <param name="db">The database context.</param>
/// <param name="ct">The cancellation token.</param>
internal static async Task<Results<Ok<OrderResponse>, ProblemHttpResult>> GetAsync(
    Guid id,
    OrdersDbContext db,
    CancellationToken ct)
{
    var order = await db.Orders
        .AsNoTracking()
        .Include(o => o.Lines)
        .SingleOrDefaultAsync(o => o.Id == id, ct);

    return order is null
        ? TypedResults.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "The order does not exist.",
            extensions: new Dictionary<string, object?> { ["code"] = "order.not_found" })
        : TypedResults.Ok(order.ToResponse());
}
```

`WithName` must be unique in the document. Use the verb and the resource: `GetOrder`, `ListOrders`, `CreateOrder`, `CancelOrder`. Add a name to each existing endpoint too, because a client generator reads all operations.

## XML comments

The .NET 10 source generator copies XML comments into the document. Tested behavior:

- It reads comments on `internal` and `public` methods and types.
- It skips comments on `private` methods and on lambdas.
- It needs `<GenerateDocumentationFile>true</GenerateDocumentationFile>`. This turns on CS1591 for each public member without a comment. With `-warnaserror` the build fails. Add `<NoWarn>$(NoWarn);CS1591</NoWarn>` in the API project only.

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);CS1591</NoWarn>
</PropertyGroup>
```

Use `.WithSummary("...")` and `.WithDescription("...")` when a method comment is not possible.

## Transformers

Microsoft.AspNetCore.OpenApi 10 uses Microsoft.OpenApi 2.x. The 1.x code that most examples show does not compile.

| 1.x (does not compile) | 2.x |
|---|---|
| `using Microsoft.OpenApi.Models;` | `using Microsoft.OpenApi;` |
| `new OpenApiString("x")` | `JsonValue.Create("x")` (`System.Text.Json.Nodes`) |
| `schema.Type = "string"` | `schema.Type = JsonSchemaType.String` |
| `schema.Nullable = true` | `schema.Type = type \| JsonSchemaType.Null` |
| `schema.Example = ...` | `schema.Examples = [...]` (`Example` is obsolete, CS0618) |
| `document.Components.SecuritySchemes["Bearer"] = ...` on a new `OpenApiComponents` | First `SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>()` |

```csharp
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Orders.Api.OpenApi;

public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        };
        return Task.CompletedTask;
    }
}

public sealed class SkuExampleTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonPropertyInfo?.Name == "sku")
        {
            schema.Examples = [JsonValue.Create("SKU-1")];
        }

        return Task.CompletedTask;
    }
}
```

Register them: `builder.Services.AddOpenApi(o => { o.AddDocumentTransformer<BearerSecurityTransformer>(); o.AddSchemaTransformer<SkuExampleTransformer>(); });`.

For one operation, use the endpoint method. It replaces `WithOpenApi` (ASPDEPR002). `WithOpenApi` still compiles and runs, but the built-in generator ignores the operation that its delegate returns. A `Summary` or `Description` in it is not in the document (tested with 10.0.13 on 2026-10-25). `AddOpenApiOperationTransformer` and `WithSummary` reach the document:

```csharp
app.MapGet("/orders/{id:guid}", GetAsync)
    .WithName("GetOrder")
    .WithSummary("Gets one order.")
    .AddOpenApiOperationTransformer((operation, context, ct) =>
    {
        operation.Deprecated = true;
        return Task.CompletedTask;
    });
```

## Schema of numbers and enums

The schema follows `ConfigureHttpJsonOptions` (minimal APIs) or `AddJsonOptions` (controllers).

| JSON options | `int` in the schema | enum in the schema |
|---|---|---|
| Web defaults | `"type": ["integer","string"]` with a pattern | `integer` |
| `NumberHandling = Strict` and `JsonStringEnumConverter` (API-11) | `integer` | `string` with the names |

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
```

A change of the JSON options changes what the API accepts and returns. In an API that has clients, it is a breaking change. Ask the human (decision API-11).

`decimal` shows as `number` with `format: double`. Tell a client team when they need exact money values: they read the value as a decimal type in their language.

## Swashbuckle in an existing project

Swashbuckle.AspNetCore 10 is maintained and uses Microsoft.OpenApi 2.x. Keep it in a project that has it (stack rule 3). Its filters (`IOperationFilter`, `ISchemaFilter`) need the 2.x types of the table above. The rules of response metadata, `WithName`, environment checks, and the build-time document apply in the same way. Move to the built-in generator only when the task asks for it.

## A test for the document

Read the document in a test with a Development host. This test finds a missing status code before the client team does.

```csharp
[TestMethod]
public async Task OpenApi_GetOrder_Documents404AsProblemJson()
{
    await using var factory = new WebApplicationFactory<Program>()
        .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
    using var client = factory.CreateClient();

    using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", TestContext.CancellationToken));
    var notFound = document.RootElement
        .GetProperty("paths").GetProperty("/orders/{id}")
        .GetProperty("get").GetProperty("responses").GetProperty("404");

    Assert.IsTrue(notFound.GetProperty("content").TryGetProperty("application/problem+json", out _));
}
```

Add a second test with `UseEnvironment("Production")`: `GET /openapi/v1.json` and `GET /scalar` return 404.
