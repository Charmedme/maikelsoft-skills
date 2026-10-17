# Outbound HTTP and resilience

Detail for `IHttpClientFactory`, typed clients, and `Microsoft.Extensions.Http.Resilience` 10.11.0 (Polly 8). Each code block compiled with .NET SDK 10.0.402 and `-warnaserror`.

## Contents

- Which client form
- Typed client
- Singletons and static clients
- The standard resilience handler
- Defaults
- Unsafe methods
- Timeouts
- Per-client changes
- What to catch
- Old method: Microsoft.Extensions.Http.Polly
- Tests

## Which client form

| Situation | Form |
|-----------|------|
| A scoped or transient service calls one API | Typed client: `AddHttpClient<TClient>()` (decision CFG-05) |
| A singleton or a hosted service calls an API | Named client: `IHttpClientFactory.CreateClient("name")` per call |
| Code without DI (a small tool) | One static `HttpClient` with `SocketsHttpHandler.PooledConnectionLifetime` |
| An API with a reliable OpenAPI document | Generated client (Kiota, NSwag), registered as a typed client |

Do not write `new HttpClient()` per call. Each instance opens its own connections, and the sockets stay in TIME_WAIT after dispose. Under load the app runs out of ports.

## Typed client

The factory creates the typed client as a transient service and gives it an `HttpClient` from a pooled handler. The handler rotates every 2 minutes by default, so DNS changes take effect.

The template client reads its base address from validated options in the constructor: `references/program-template.md`, section "Typed client and health check".

- Pass the `CancellationToken` to each call.
- Escape values in the path: `Uri.EscapeDataString(id)`.
- Use `GetFromJsonAsync`, `PostAsJsonAsync`, and `ReadFromJsonAsync`. They use the web JSON defaults.
- Dispose each `HttpResponseMessage` that you read by hand (`using var response = ...`).

## Singletons and static clients

A typed client in a singleton keeps the first handler for the life of the app and does not see DNS changes. Inject `IHttpClientFactory` and create the client per call:

```csharp
namespace Api.Snippets;

// A singleton that calls HTTP: inject IHttpClientFactory and create the client per call.
public sealed class RatesCache(IHttpClientFactory factory)
{
    public async Task<string> RefreshAsync(CancellationToken cancellationToken)
    {
        var client = factory.CreateClient("rates");
        return await client.GetStringAsync("latest", cancellationToken);
    }
}

// Code without DI (for example a small tool): one static client with a pooled connection lifetime.
public static class StaticHttp
{
    public static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
}

public static class HttpSetup
{
    public static IServiceCollection AddRates(this IServiceCollection services)
    {
        services.AddHttpClient("rates", client =>
        {
            client.BaseAddress = new Uri("https://rates.example/");
            // The resilience pipeline owns the timeouts. HttpClient.Timeout (100 s) must not cut it short.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        services.AddSingleton<RatesCache>();
        return services;
    }
}
```

## The standard resilience handler

Add one handler to all clients:

```csharp
builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
builder.Services.AddHttpClient<PaymentClient>();
```

`AddStandardResilienceHandler` extends `IHttpClientBuilder`, not `IServiceCollection`. Add one resilience handler per client. Two handlers multiply the retries and the timeouts. When one client needs other settings, read "Per-client changes".

## Defaults

The standard handler chains five strategies, from outside to inside:

| Strategy | Default |
|----------|---------|
| Rate limiter (bulkhead) | 1000 concurrent requests |
| Total request timeout | 30 s, including all retries |
| Retry | 3 retries, exponential backoff with jitter, starts at 2 s. Retries on 408, 429, 5xx, `HttpRequestException`, and `TimeoutRejectedException`. Honors `Retry-After` |
| Circuit breaker | opens at a 10% failure ratio over 30 s with at least 100 requests, stays open 5 s |
| Attempt timeout | 10 s per attempt |

## Unsafe methods

The standard handler retries all methods, also POST. A POST that times out at the client can still succeed on the server, and the retry then writes a second time.

Turn off retries for calls that are not idempotent. This client replaces the default handler from `ConfigureHttpClientDefaults` (see "Per-client changes"):

```csharp
using Microsoft.Extensions.Http.Resilience;

namespace Api.Infrastructure;

public sealed class ShippingClient(HttpClient http)
{
    public Task<HttpResponseMessage> BookAsync(HttpContent body, CancellationToken cancellationToken) =>
        http.PostAsync("bookings", body, cancellationToken);
}

public static class ResilienceSetup
{
    public static IServiceCollection AddShipping(this IServiceCollection services)
    {
        var shipping = services.AddHttpClient<ShippingClient>(
            client => client.BaseAddress = new Uri("https://shipping.example"));

        // Program.cs adds a standard handler to each client. A second handler stacks:
        // 4 attempts become 16. Remove the default first. The API is experimental in 10.11.0.
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers: replace the default handler for this client only.
        shipping.RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

        shipping.AddStandardResilienceHandler(options =>
        {
            // POST is not idempotent here: no retry for unsafe methods.
            options.Retry.DisableForUnsafeHttpMethods();
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
        });
        return services;
    }
}
```

- `DisableForUnsafeHttpMethods()` excludes POST, PATCH, PUT, DELETE, and CONNECT. `DisableFor(HttpMethod.Post, HttpMethod.Patch)` keeps retries for idempotent PUT and DELETE.
- The method is in the namespace `Microsoft.Extensions.Http.Resilience`. With version 10.11.0 it is stable: the build gave no EXTEXP0001 diagnostic (checked 2026-10-16).
- A POST with an idempotency key (decision API-13) can keep its retries.

## Timeouts

| Timeout | Default | Exception |
|---------|---------|-----------|
| `HttpClient.Timeout` | 100 s | `TaskCanceledException` with an inner `TimeoutException` |
| Attempt timeout | 10 s | none: the handler retries |
| Total request timeout | 30 s | `Polly.Timeout.TimeoutRejectedException` |

`HttpClient.Timeout` applies around the whole pipeline. When you raise the total timeout above 100 s, also raise `HttpClient.Timeout` or set it to `Timeout.InfiniteTimeSpan`. Else the client stops the request before the pipeline.

## Per-client changes

Two cases:

- No defaults in `ConfigureHttpClientDefaults`: pass options to the `AddStandardResilienceHandler(options => ...)` call of the client.
- Defaults in `ConfigureHttpClientDefaults`: a second `AddStandardResilienceHandler` on the client stacks. A test with an always-failing server counted 16 attempts instead of 4. Call `RemoveAllResilienceHandlers()` first, as the example in "Unsafe methods" does.

`RemoveAllResilienceHandlers()` is experimental in 10.11.0. The build reports EXTEXP0001 as an error. Suppress it around the one call with a reason. A `#pragma` inside a fluent chain does not cover the call, so call it as a separate statement.

Keep the attempt timeout smaller than the total timeout. Keep the total timeout smaller than the time that the caller waits, for example the request timeout of your own endpoint.

For a fully custom pipeline, use `AddResilienceHandler("name", pipeline => ...)` with the Polly 8 builder. Use it only when the standard strategies do not fit.

## What to catch

- `TimeoutRejectedException` derives from `Exception`. `catch (HttpRequestException)` does not catch it.
- After the last retry, the handler returns the last error response. It does not throw. `EnsureSuccessStatusCode()` then throws `HttpRequestException`.
- A `TaskCanceledException` is a timeout only when its inner exception is `TimeoutException`. Else the caller cancelled: stop quietly.
- An open circuit throws `BrokenCircuitException`. Return a 503 ProblemDetails, or a fallback.

## Old method: Microsoft.Extensions.Http.Polly

Code with `AddTransientHttpErrorPolicy`, `AddPolicyHandler`, and `Policy.Handle<...>().WaitAndRetryAsync(...)` uses `Microsoft.Extensions.Http.Polly`, which depends on Polly 7.2.4. The package still ships, but new code uses `Microsoft.Extensions.Http.Resilience` (Polly 8). To move:

1. Remove the policies and the package.
2. Add `Microsoft.Extensions.Http.Resilience` (ask the human, stack rule 5).
3. Add `AddStandardResilienceHandler()`, and copy the custom values (retry count, timeouts) into its options.
4. Run the HTTP tests.

## Tests

- Unit test a typed client with a fake `HttpMessageHandler` that returns fixed responses.
- Test the behavior, not the Polly settings: "a GET retries on 503", "a POST does not retry".
- Do not test the defaults of the library.
- Details: `dev-stack:dotnet-testing`.
