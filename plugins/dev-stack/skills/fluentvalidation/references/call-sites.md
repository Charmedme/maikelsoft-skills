# Call sites and the error response

Where a validator runs, and how its result reaches the client. Each C# example compiled with SDK 10.0.402 and FluentValidation 12.2.0. The filter ran in the `orders-api` eval fixture: a null body in Production, an async rule, and the code extension each gave a 400.

## Contents

- Minimal APIs: one generic endpoint filter
- The error shape (decision API-20)
- Next to the built-in AddValidation
- Controllers
- Workers and message handlers
- Null and Development

## Minimal APIs: one generic endpoint filter

```csharp
using FluentValidation;
using FluentValidation.Results;

namespace Orders.Api.Orders;

// Runs the FluentValidation validator of T before the handler.
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } argument)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "The request body is missing.",
                extensions: new Dictionary<string, object?> { ["code"] = "request.body_missing" });
        }

        var result = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : result.ToValidationProblem();
    }
}
```

Use it on the endpoint, and document the 400:

```csharp
group.MapPost("/", CreateAsync)
    .WithName("CreateOrder")
    .AddEndpointFilter<ValidationFilter<CreateOrderRequest>>()
    .ProducesValidationProblem();
```

- The filter needs no package beyond FluentValidation. Auto-validation packages (`FluentValidation.AspNetCore`, SharpGrip) add a dependency for these few lines.
- Remove a limit check from the handler when the validator has it, so that one place owns the rule.
- `IValidator<T>` must be registered. A missing registration (for example an `internal` validator) fails when the endpoint runs, not at startup. A test with the real validator finds it.

## The error shape (decision API-20)

`result.ToDictionary()` gives `field -> messages`. The codes need an extension:

```csharp
using FluentValidation.Results;

namespace Orders.Api.Orders;

public static class ValidationResultExtensions
{
    // Same shape as decision API-20: messages in "errors", stable codes in "errorCodes".
    public static IResult ToValidationProblem(this ValidationResult result) =>
        TypedResults.ValidationProblem(
            result.ToDictionary(),
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "validation.failed",
                ["errorCodes"] = result.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorCode).Distinct().ToArray()),
            });
}
```

The response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Lines[0].Sku": ["The SKU is not a known product."] },
  "code": "validation.failed",
  "errorCodes": { "Lines[0].Sku": ["order.sku_unknown"] },
  "traceId": "00-..."
}
```

- `errors` keeps the standard shape, so a generic client still shows the messages.
- The client acts on `errorCodes`, never on the message text. The messages can change and can be translated (decision CFG-10 in `dev-stack:aspnetcore`).
- When the project also uses the built-in `AddValidation` for other types, give its 400 the same `code` (decision API-20), so that the client reads one shape.

## Next to the built-in AddValidation

`builder.Services.AddValidation()` adds a filter to each minimal API endpoint whose parameters have DataAnnotations. It runs before your filter.

| Request type has | Result |
|---|---|
| DataAnnotations and a validator | The built-in filter answers first for the annotated fields, without codes. The validator does not run for them |
| A validator only | The validator answers |
| DataAnnotations only | The built-in filter answers |

Pick one library per type. When a type moves to FluentValidation, remove its attributes. When the attributes must stay (for example a shared contract package), call `.DisableValidation()` on the endpoint.

## Controllers

Inject the validator and call it in the action. `[ApiController]` model validation still runs for DataAnnotations, so the same one-library rule applies.

```csharp
[ApiController]
[Route("api/legacy/orders")]
public sealed class OrdersController(IValidator<CreateOrderRequest> validator) : ControllerBase
{
    [HttpPost]
    public async Task<IResult> CreateAsync(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid)
        {
            return result.ToValidationProblem();
        }

        return TypedResults.NoContent();
    }
}
```

## Workers and message handlers

A worker uses the same validator. Resolve it from a scope, because the validator is scoped:

```csharp
public sealed partial class ImportOrdersWorker(
    Channel<ImportOrderMessage> queue,
    IServiceScopeFactory scopes,
    ILogger<ImportOrdersWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken))
        {
            await using var scope = scopes.CreateAsyncScope();
            var validator = scope.ServiceProvider.GetRequiredService<IValidator<CreateOrderRequest>>();
            var result = await validator.ValidateAsync(message.Order, stoppingToken);
            if (!result.IsValid)
            {
                LogRejected(message.ExternalId, string.Join(",", result.Errors.Select(e => e.ErrorCode)));
                continue;
            }

            // Create the order here.
        }
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Import {ExternalId} rejected: {ErrorCodes}")]
    private partial void LogRejected(string externalId, string errorCodes);
}
```

- A rejected message is a business outcome, not a retry case. Do not throw, so that the message is not retried forever.
- Log the codes, not the values: a value can hold personal data.

## Null and Development

Tested with .NET 10 and a filter on a required body parameter:

| Environment | JSON body `null` |
|---|---|
| Production | The filter runs, and its argument is null. Without a null check, `ValidateAsync(null)` throws `InvalidOperationException`: a 500 |
| Development (also the default of `WebApplicationFactory`) | `ThrowOnBadRequest` throws `BadHttpRequestException` before the filters: a 500 from `UseExceptionHandler`, for each solution |

So test the null body with `UseEnvironment("Production")` in the test host. A nullable parameter (`CreateOrderRequest? request`) reaches the filter as null in both environments.
