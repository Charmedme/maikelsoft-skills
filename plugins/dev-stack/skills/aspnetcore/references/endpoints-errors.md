# Endpoints, errors, validation, JSON

Detail for minimal API endpoints, request and response types, ProblemDetails, exception handlers, validation, and JSON settings. Each code block compiled with .NET SDK 10.0.402 and `-warnaserror`. The full feature example is in `references/program-template.md`, section "Feature endpoints and handler".

## Contents

- Endpoint shape
- TypedResults and Results unions
- Status codes
- Request and response types
- Handlers and mapping
- Endpoint filters
- Errors: the ProblemDetails setup
- Expected failures
- IExceptionHandler
- .NET 10: handled exceptions are not logged
- StatusCodeSelector
- Validation with AddValidation
- Validation with FluentValidation
- JSON settings
- Controllers in an existing code base
- OpenAPI and versioning

## Endpoint shape

- One static class per feature with `MapX(this IEndpointRouteBuilder)` and a `MapGroup` for the shared prefix, tags, and policies.
- Named static handler methods, not long lambdas. XML comments on named methods reach OpenAPI, a stack trace shows the method name, and a unit test can call the method.
- Give each endpoint a stable name with `.WithName("GetOrder")`. Client generators and `CreatedAtRoute` use it.
- Put shared metadata on the group: `.RequireAuthorization(...)`, `.RequireRateLimiting(...)`, `.ProducesProblem(...)`.
- Bind parameters from the route, the query, the body, and DI. Use `[AsParameters]` to group many query parameters in one record.

## TypedResults and Results unions

`TypedResults.Ok(x)` returns `Ok<T>`. `Results.Ok(x)` returns `IResult`. The typed form gives OpenAPI the response type and lets a unit test check the result without a server.

When a handler has more than one outcome, declare the union:

```csharp
private static async Task<Results<Ok<OrderResponse>, NotFound>> GetById(
    Guid id, OrderHandler orders, CancellationToken cancellationToken) =>
    await orders.FindAsync(id, cancellationToken) is { } order
        ? TypedResults.Ok(order)
        : TypedResults.NotFound();
```

A lambda with a ternary of two `TypedResults` and no declared return type does not compile. The compiler cannot find a common type and tries `RequestDelegate`: the build gives CS1678 and CS1661.

`TypedResults.Problem(...)` in a union does not add the status code to the OpenAPI document. Add `.ProducesProblem(409)` on the endpoint or the group.

For a handler with many outcomes (about 7 or more), `IResult` with `Results.X` is acceptable. Declare the status codes with `.Produces<T>(200)` and `.ProducesProblem(...)`. Suppress `ASP-RESULTS-UNTYPED` on that line with a reason.

## Status codes

| Operation | Success | Common errors |
|-----------|---------|---------------|
| GET one item | 200 | 404 |
| GET list | 200 (empty list, not 404) | 400 (bad paging) |
| POST create | 201 with `Location` | 400, 409 |
| POST action (`/orders/{id}/cancel`) | 200 or 204 | 404, 409 |
| PUT replace | 200 or 204 | 400, 404, 409 |
| PATCH | 200 | 400, 404, 409 |
| DELETE | 204 | 404, 409 |
| Any | | 401 no user, 403 no permission, 429 rate limit |

## Request and response types

```csharp
public sealed record OrderResponse(Guid Id, string Sku, int Quantity, OrderStatus Status, DateTimeOffset CreatedAt);

public sealed record CreateOrderRequest(
    [property: Required, MaxLength(32)] string Sku,
    [property: Range(1, 1000)] int Quantity);
```

- One record per request and per response (decision API-03). A request has no `Id` or `CreatedAt` that the server sets, so a client cannot over-post them.
- `DateTimeOffset` for instants. It keeps the offset and writes ISO 8601 with the offset.
- `AddValidation` reads the attributes on positional record parameters with or without `property:`. A run confirmed both forms. Use `property:` when the attributes must also show on the property, for example for MVC.
- Add `<summary>` comments to public request and response types when the project generates OpenAPI from XML comments (`dev-stack:aspnetcore-openapi`).

## Handlers and mapping

- The endpoint injects one handler class per use case and calls it (decision API-06). The handler holds the logic and the data access. MediatR is not allowed.
- Map by hand: a static `ToResponse()` extension method, a constructor, or a LINQ projection `Select(x => new OrderResponse(...))` that lets EF read only the needed columns. For large mappings, Mapperly (decision API-07). AutoMapper is not allowed.

## Endpoint filters

An endpoint filter runs before and after one endpoint or a group. Use it for cross-cutting work: validation, an idempotency key, audit. It replaces a mediator pipeline behavior.

```csharp
group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

Filters run in the order of the `AddEndpointFilter` calls. A filter on a group runs before a filter on an endpoint.

## Errors: the ProblemDetails setup

```csharp
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
```

- `UseExceptionHandler` catches each unhandled exception and writes a ProblemDetails 500 response.
- `UseStatusCodePages` gives a ProblemDetails body to an empty error response, for example a bare `TypedResults.NotFound()` or the 401 from authentication.
- `CustomizeProblemDetails` adds fields to each ProblemDetails. A run of the template returned `{"title":"Unauthorized","status":401,"traceId":"..."}` with `Content-Type: application/problem+json`.
- No try/catch in endpoints. A local try/catch is right only to change one known exception into one result.

## Expected failures

An expected failure is a result, not an exception (decision API-05). The handler returns a small result, and the endpoint maps it:

```csharp
private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> Cancel(
    Guid id, OrderHandler orders, CancellationToken cancellationToken) =>
    await orders.CancelAsync(id, cancellationToken) switch
    {
        CancelResult.Done => TypedResults.NoContent(),
        CancelResult.NotFound => TypedResults.NotFound(),
        _ => TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "The order is already shipped.",
            extensions: new Dictionary<string, object?> { ["code"] = "order.already_shipped" }),
    };
```

The `code` extension is stable. Clients act on it and translate it (decisions API-04, CFG-10). The `title` is English text for people.

## IExceptionHandler

Use an `IExceptionHandler` to map infrastructure exceptions to a status code. Return `false` for an exception that the handler does not know: the next handler or the default 500 response takes it.

```csharp
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Infrastructure;

public sealed class ConcurrencyConflictException(string message) : Exception(message);

// Maps known infrastructure exceptions to a status code. Unknown exceptions go to the default 500 handler.
internal sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ConcurrencyConflictException)
        {
            return false;
        }

        // .NET 10: the middleware does not log an exception that a handler handles. Log it here.
        LogConflict(exception, httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "The resource was changed by another request.",
                Extensions = { ["code"] = "concurrency.conflict" },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict on {Path}")]
    private partial void LogConflict(Exception exception, string path);
}
```

- Write the response through `IProblemDetailsService`, so `CustomizeProblemDetails` also runs.
- Do not use `exception.Message` as the detail. The message can contain SQL, paths, or personal data.
- Register more than one handler when needed. They run in registration order.

## .NET 10: handled exceptions are not logged

From .NET 10, when `TryHandleAsync` returns `true`, the exception handler middleware does not:

- log `UnhandledException`,
- write the `HandledException` event,
- add the `error.type` tag to the `http.server.request.duration` metric.

What to do: log the exception in the handler, as the example does. Or keep the old behavior for each handled exception with `SuppressDiagnosticsCallback = _ => false` (next section).

## StatusCodeSelector

When only the status code changes, `StatusCodeSelector` is shorter than a handler:

```csharp
using Microsoft.AspNetCore.Diagnostics;

namespace Api.Snippets;

public static class ErrorSetup
{
    // Alternative to an IExceptionHandler when only the status code changes.
    public static IApplicationBuilder UseApiExceptionHandler(this IApplicationBuilder app) =>
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = exception => exception switch
            {
                TimeoutException => StatusCodes.Status504GatewayTimeout,
                _ => StatusCodes.Status500InternalServerError,
            },
            // .NET 10: keep the framework log and the error.type metric for handled exceptions.
            SuppressDiagnosticsCallback = _ => false,
        });
}
```

## Validation with AddValidation

.NET 10 minimal APIs (decision API-08):

```csharp
builder.Services.AddValidation();
```

- A source generator finds the types of the handler parameters and writes the validation code. Invalid input gets a 400 ProblemDetails with an `errors` object per field. A run returned `"errors":{"Sku":["The field Sku must be ..."],"Qty":["The field Qty must be between 1 and 10."]}`.
- It supports DataAnnotations attributes, `IValidatableObject`, nested objects, and collections.
- The generator sees only the types in the assembly that calls `AddValidation`. For types in a class library, mark them `[ValidatableType]`. In .NET 10 this attribute is experimental (ASP0029). In .NET 11 it is stable.
- It does not apply to MVC controllers. `[ApiController]` model validation does that work.
- `[SkipValidation]` turns it off for one parameter or type.
- It does not run FluentValidation validators.

Keep rules about shape in the request: required, length, range, format. Keep rules about state (an order can only ship when paid) in the handler or the domain. Other paths, such as jobs and imports, must also follow them. A uniqueness check needs a unique index in the database, because two requests can pass a check at the same time.

## Validation with FluentValidation

When the project uses FluentValidation, validate explicitly. The owner deprecated the package `FluentValidation.AspNetCore` and `AddFluentValidationAutoValidation()`. They are MVC only and run async rules synchronously. For minimal APIs, one generic endpoint filter does the work:

```csharp
using FluentValidation;

namespace Api.Infrastructure;

// Runs the FluentValidation validator of T before the handler. Use only when the project uses FluentValidation.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status400BadRequest, title: "The request body is missing.");
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
```

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
group.MapPost("/", Create).AddEndpointFilter<ValidationFilter<CreateOrderRequest>>();
```

- `AddValidatorsFromAssemblyContaining` is in the namespace `FluentValidation` (package `FluentValidation.DependencyInjectionExtensions`).
- Always call `ValidateAsync` with the request token. `Validate` throws when a validator has an async rule.
- In a controller or in code without HTTP, inject `IValidator<T>` and call `ValidateAsync` in the method.
- Do not validate one type with both FluentValidation and `AddValidation`. The client then gets two error formats.

The rules for validators (cascade mode, error codes, rule sets, tests) are in `dev-stack:fluentvalidation`.

## JSON settings

Minimal APIs read `ConfigureHttpJsonOptions`. Controllers read `AddJsonOptions`. They are two different option objects: set both when a project has both.

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
```

- The web defaults: camelCase names, case-insensitive read, numbers readable from strings.
- `JsonStringEnumConverter` writes enum names, not numbers. Removing an enum value is a breaking change for clients.
- `NumberHandling.Strict` rejects `"5"` for a number. The OpenAPI schema then says `integer`, not `["integer","string"]`.
- New projects can also reject duplicate properties (`AllowDuplicateProperties = false`) and match names with the exact case (`PropertyNameCaseInsensitive = false`). In an existing API, keep the settings, because each change can break a client.
- A custom `JsonConverter` must handle `Utf8JsonReader.HasValueSequence`. From .NET 10, MVC and minimal APIs read JSON from a `PipeReader`, and a converter that ignores the sequence can lose data.

## Controllers in an existing code base

Keep controllers when the project has them (decision API-01). The rules are the same: records, ProblemDetails, a token in each action, a handler class. Differences:

- `[ApiController]` validates the model and returns a ProblemDetails 400. `AddValidation` does not apply.
- Return `ActionResult<T>` and declare each status code with `[ProducesResponseType]`.
- A create returns `CreatedAtRoute` or `CreatedAtAction`.

Example: `references/program-template.md`, section "Controller".

## OpenAPI and versioning

Short rules. The full rules are in `dev-stack:aspnetcore-openapi`.

- `builder.Services.AddOpenApi()` and `app.MapOpenApi()` inside `if (app.Environment.IsDevelopment())`. A UI (Scalar or Swagger UI) also only in Development (decision API-09).
- Do not add Swashbuckle to a new project. Keep it where it exists.
- Do not call `.WithOpenApi()`. Microsoft deprecated it (ASPDEPR002), and it does not feed the built-in generator.
- Declare each status code: a `Results<...>` union, `.ProducesProblem(status)`, `.ProducesValidationProblem()`.
- With a fallback authorization policy, mark the document endpoint `AllowAnonymous()` or protect it on purpose.
- Versioning (decision API-10): no versioning for an internal API with one consumer. Else ask, and use `dev-stack:aspnetcore-openapi`.
