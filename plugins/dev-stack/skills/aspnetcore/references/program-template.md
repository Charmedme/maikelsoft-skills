# Program template (.NET 10)

A complete `Program.cs` for a new API, with the files it uses. Each block compiled with the .NET SDK 10.0.402, `-warnaserror`, and `AnalysisLevel=latest-recommended`. A run confirmed the behavior: a missing `Payment:BaseUrl` stops the start, `/healthz/live` returns 200 without a token, and other paths return a 401 ProblemDetails.

Packages: `Microsoft.AspNetCore.OpenApi`, `Microsoft.Extensions.Http.Resilience`, `Microsoft.AspNetCore.Authentication.JwtBearer`. Each new package needs the approval of the human (`references/stack-rules.md`, section 5).

## Contents

- Program.cs
- Feature endpoints and handler
- Options class
- Typed client and health check
- Exception handler
- Background service
- Shared resilience settings for one client
- Worker service (no web)
- Controller (existing controller code only)

## Program.cs

The order of the `Use...` calls is the behavior. Change it only with a reason.

```csharp
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Api.Features.Orders;
using Api.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Catch captive dependencies and missing registrations at startup in each environment.
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

// Errors: ProblemDetails for every error response.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

// Validation of minimal API parameters (DataAnnotations).
builder.Services.AddValidation();

// JSON for minimal APIs.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddOpenApi();
builder.Services.AddSingleton(TimeProvider.System);

// Options: bind, validate, fail at startup.
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(PaymentOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Outbound HTTP: one resilience handler for each client.
builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());
builder.Services.AddHttpClient<PaymentClient>();

// Health.
builder.Services.AddHealthChecks()
    .AddCheck<PaymentApiHealthCheck>("payment-api", tags: ["ready"]);

// Background work.
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddScoped<IOutbox, Outbox>();
builder.Services.AddOrders();
builder.Services.AddShipping();

// Behind a reverse proxy: trust only the known proxy network.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
});

// CORS: one named policy, exact origins from configuration.
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("web", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// Authentication and authorization: secure by default.
builder.Services.AddAuthentication().AddJwtBearer();
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("orders:write", policy => policy.RequireClaim("scope", "orders.write"));

// Rate limiting: named policy on sensitive endpoints, 429 to the client.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("per-user", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 100, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseCors("web");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false }).AllowAnonymous().ShortCircuit();
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") })
    .AllowAnonymous().ShortCircuit();

app.MapOrders();

app.Run();
```

## Feature endpoints and handler

One static class per feature. Named static handler methods, so XML comments reach OpenAPI and tests can call them. `Results<...>` declares each outcome.

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Features.Orders;

public enum OrderStatus { Draft, Placed, Shipped }

public sealed record OrderResponse(Guid Id, string Sku, int Quantity, OrderStatus Status, DateTimeOffset CreatedAt);

public sealed record CreateOrderRequest(
    [property: Required, MaxLength(32)] string Sku,
    [property: Range(1, 1000)] int Quantity);

public static class OrderEndpoints
{
    public static IServiceCollection AddOrders(this IServiceCollection services) =>
        services.AddScoped<OrderHandler>();

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders").WithTags("Orders");

        group.MapGet("/{id:guid}", GetById).WithName("GetOrder");
        group.MapPost("/", Create)
            .WithName("CreateOrder")
            .RequireAuthorization("orders:write")
            .RequireRateLimiting("per-user");
        group.MapPost("/{id:guid}/cancel", Cancel).WithName("CancelOrder");

        return app;
    }

    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetById(
        Guid id, OrderHandler orders, CancellationToken cancellationToken) =>
        await orders.FindAsync(id, cancellationToken) is { } order
            ? TypedResults.Ok(order)
            : TypedResults.NotFound();

    private static async Task<Created<OrderResponse>> Create(
        CreateOrderRequest request, OrderHandler orders, CancellationToken cancellationToken)
    {
        var order = await orders.CreateAsync(request, cancellationToken);
        return TypedResults.Created($"/api/orders/{order.Id}", order);
    }

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
}

public enum CancelResult { Done, NotFound, AlreadyShipped }

public sealed class OrderHandler(TimeProvider time)
{
    public Task<OrderResponse?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<OrderResponse?>(null);

    public Task<OrderResponse> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new OrderResponse(Guid.CreateVersion7(), request.Sku, request.Quantity,
            OrderStatus.Placed, time.GetUtcNow()));

    public Task<CancelResult> CancelAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(CancelResult.NotFound);
}
```

## Options class

```csharp
using System.ComponentModel.DataAnnotations;

namespace Api.Infrastructure;

public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    [Required]
    public required Uri BaseUrl { get; set; }

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;
}
```

## Typed client and health check

The typed client gets its settings from `IOptions<T>` in the constructor. The resilience handler comes from `ConfigureHttpClientDefaults` in `Program.cs`.

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure;

public sealed record PaymentResult(string Id, string Status);

public sealed class PaymentClient
{
    private readonly HttpClient _http;

    public PaymentClient(HttpClient http, IOptions<PaymentOptions> options)
    {
        _http = http;
        _http.BaseAddress = options.Value.BaseUrl;
    }

    public Task<PaymentResult?> GetAsync(string id, CancellationToken cancellationToken) =>
        _http.GetFromJsonAsync<PaymentResult>($"payments/{Uri.EscapeDataString(id)}", cancellationToken);

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync("health", cancellationToken);
        return response.IsSuccessStatusCode;
    }
}

public sealed class PaymentApiHealthCheck(PaymentClient client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await client.PingAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Payment API does not respond.");
}
```

## Exception handler

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

## Background service

```csharp
namespace Api.Infrastructure;

public interface IOutbox
{
    Task PublishPendingAsync(CancellationToken cancellationToken);
}

public sealed class Outbox : IOutbox
{
    public Task PublishPendingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed partial class OutboxPublisher(
    IServiceScopeFactory scopes, TimeProvider time, ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
                await outbox.PublishPendingAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPublishFailed(ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox publish failed")]
    private partial void LogPublishFailed(Exception exception);
}
```

## Shared resilience settings for one client

Retries off for unsafe methods, and shorter timeouts. `Program.cs` already adds a default handler to each client, so this client removes it first. Else the two handlers stack (16 attempts instead of 4). `RemoveAllResilienceHandlers` is experimental (EXTEXP0001) in 10.11.0. `DisableForUnsafeHttpMethods` is stable.

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

## Worker service (no web)

`Host.CreateApplicationBuilder` gives the same configuration, logging, and DI without Kestrel.

```csharp
using Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<HostOptions>(options =>
{
    // Keep under the grace period of the orchestrator (Kubernetes default: 30 s).
    options.ShutdownTimeout = TimeSpan.FromSeconds(25);
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<CacheWarmer>();

var host = builder.Build();
await host.RunAsync();
```

```csharp
namespace Worker;

// .NET 10: ExecuteAsync runs on a background thread. Work that must finish before
// the host starts goes in StartAsync.
public sealed partial class CacheWarmer(ILogger<CacheWarmer> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await WarmAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogStopping();
        }
    }

    private static Task WarmAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Cache warmer stops")]
    private partial void LogStopping();
}
```

## Controller (existing controller code only)

Use this shape when the project already has controllers (decision API-01). `[ApiController]` turns on model validation and the ProblemDetails 400 response.

```csharp
using System.Text.Json.Serialization;
using Mvc.Features.Invoices;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Controllers use their own JSON options object, not ConfigureHttpJsonOptions.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });
builder.Services.AddScoped<InvoiceHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapControllers();

app.Run();
```

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Mvc.Features.Invoices;

public sealed record InvoiceResponse(Guid Id, decimal Amount);

public sealed record CreateInvoiceRequest([property: Range(0.01, 1_000_000)] decimal Amount);

public sealed class InvoiceHandler
{
    public Task<InvoiceResponse?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<InvoiceResponse?>(null);

    public Task<InvoiceResponse> CreateAsync(CreateInvoiceRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new InvoiceResponse(Guid.CreateVersion7(), request.Amount));
}

// Existing controller code base: [ApiController] gives model validation and ProblemDetails for 400.
[ApiController]
[Route("api/invoices")]
public sealed class InvoicesController(InvoiceHandler invoices) : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetInvoice")]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceResponse>> GetById(Guid id, CancellationToken cancellationToken) =>
        await invoices.FindAsync(id, cancellationToken) is { } invoice ? Ok(invoice) : NotFound();

    [HttpPost]
    [ProducesResponseType<InvoiceResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceResponse>> Create(
        CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var invoice = await invoices.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetInvoice", new { id = invoice.Id }, invoice);
    }
}
```
