# Hosting, DI, and options

Detail for the host, the DI container, configuration, options, hosted services, and shutdown. Each code block compiled with .NET SDK 10.0.402 and `-warnaserror`.

## Contents

- Host builders
- DI lifetimes
- Scope validation
- Registration style
- Configuration sources
- Options: bind and validate
- Nested options and source-generated validators
- Which options interface to inject
- .NET 10: null values in configuration
- Hosted services and BackgroundService
- .NET 10: ExecuteAsync runs in the background
- Shutdown
- Old method

## Host builders

| App | Builder |
|-----|---------|
| Web API, web app | `WebApplication.CreateBuilder(args)` |
| Worker, queue consumer, console app with DI | `Host.CreateApplicationBuilder(args)` |
| Test of the web app | `WebApplicationFactory<Program>` (`dev-stack:dotnet-testing`) |

Both builders give configuration (JSON, environment variables, user-secrets in Development, command line), logging, DI, and the host lifetime. `WebHost`, `WebHostBuilder`, and `IWebHost` are obsolete in .NET 10 (ASPDEPR008).

## DI lifetimes

| Lifetime | One instance per | Use for |
|----------|------------------|---------|
| Singleton | app | stateless services, caches, `TimeProvider`, `IHttpClientFactory` consumers |
| Scoped | request, or `CreateAsyncScope()` | `DbContext`, unit of work, handlers that use a `DbContext` |
| Transient | resolve | light services with no state |

A service can take a dependency with the same or a longer lifetime. A singleton that takes a scoped service keeps one instance for all requests (captive dependency). A `DbContext` then sees many threads and returns old data.

A hosted service is a singleton. It creates a scope for each unit of work:

```csharp
await using var scope = scopes.CreateAsyncScope();
var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
```

Use `CreateAsyncScope()`, not `CreateScope()`, because a scoped service can implement only `IAsyncDisposable`.

## Scope validation

Development turns on `ValidateScopes` and `ValidateOnBuild`. Production does not. Turn them on in each environment, so that a captive dependency or a missing registration stops the start:

```csharp
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
```

`ValidateOnBuild` cannot see services that code resolves with `GetRequiredService` at run time, or factory registrations. A test that starts the host covers more (`dev-stack:dotnet-testing`).

## Registration style

- One `AddX(this IServiceCollection)` method per feature, next to the feature. `Program.cs` calls it.
- Register a concrete class. Add an interface only at a real seam: I/O to replace in tests, or more than one implementation. An interface for each class adds files and no value.
- Read settings in a registration with the factory overload: `services.AddSingleton(sp => new X(sp.GetRequiredService<IOptions<XOptions>>().Value))`. Do not call `BuildServiceProvider()`: it makes a second container with a second set of singletons. The analyzer ASP0000 reports it only in some call sites, so `ASP-BUILD-PROVIDER` reports each one.
- Keyed services (`AddKeyedSingleton<T>("key")`, `[FromKeyedServices("key")]`) select one of several implementations of one interface.

## Configuration sources

The default order, from low to high priority: `appsettings.json`, `appsettings.{Environment}.json`, user-secrets (Development only), environment variables, command line. A later source overwrites an earlier one.

- Environment variables use `__` for the section separator: `Payment__BaseUrl`.
- Secrets: user-secrets in Development, environment variables or orchestrator secrets in production, or a vault provider (decision CFG-02). Not in `appsettings.json`, because the file goes into the repository and the image.
- Read `IConfiguration` only in `Program.cs` and in registration code. Services read typed options.

## Options: bind and validate

Bind a section to a class, validate it, and validate at the start:

```csharp
builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration(PaymentOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

`AddOptionsWithValidateOnStart<T>()` is a short form of `AddOptions<T>()...ValidateOnStart()`.

Without `ValidateOnStart`, validation runs at the first `.Value` read. The app starts, reports healthy, and fails on the first request that needs the setting. A run of the template with no `Payment:BaseUrl` stopped at the start with `OptionsValidationException: ... The BaseUrl field is required.`

For a rule across two properties, add `.Validate(o => o.Min <= o.Max, "Min must not be more than Max.")`, or implement `IValidatableObject` on the options class.

## Nested options and source-generated validators

DataAnnotations validation does not go into nested objects or lists by default. `[ValidateObjectMembers]` and `[ValidateEnumeratedItems]` turn it on. The `[OptionsValidator]` source generator writes the validator at build time, without reflection, so it works with trimming and native AOT:

```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Api.Snippets;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    [Required]
    public required string Host { get; set; }

    [Range(1, 65535)]
    public int Port { get; set; } = 587;

    // Nested objects and lists are not validated unless you say so.
    [ValidateObjectMembers]
    public RetryOptions Retry { get; set; } = new();

    [ValidateEnumeratedItems]
    public List<Recipient> Admins { get; set; } = [];
}

public sealed class RetryOptions
{
    [Range(0, 10)]
    public int MaxAttempts { get; set; } = 3;
}

public sealed class Recipient
{
    [Required, EmailAddress]
    public required string Address { get; set; }
}

// Source-generated validator: no reflection, works with trimming and native AOT.
[OptionsValidator]
public sealed partial class MailOptionsValidator : IValidateOptions<MailOptions>;

public static class OptionsSetup
{
    public static IServiceCollection AddMail(this IServiceCollection services)
    {
        services.AddOptions<MailOptions>()
            .BindConfiguration(MailOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MailOptions>, MailOptionsValidator>();
        return services;
    }
}

// A singleton that must see configuration changes reads IOptionsMonitor<T>, not IOptionsSnapshot<T>.
public sealed class MailSender(IOptionsMonitor<MailOptions> options)
{
    public string CurrentHost => options.CurrentValue.Host;
}
```

## Which options interface to inject

| Interface | Lifetime | Sees changes | Use in |
|-----------|----------|--------------|--------|
| `IOptions<T>` | singleton | no | most services |
| `IOptionsSnapshot<T>` | scoped | per request | scoped services that need the value of this request |
| `IOptionsMonitor<T>` | singleton | yes, `CurrentValue` and `OnChange` | singletons and hosted services that must see changes |

Do not inject `IOptionsSnapshot<T>` into a singleton. `ValidateScopes` reports it.

## .NET 10: null values in configuration

Before .NET 10, the binder skipped a `null` value, and the JSON provider changed `null` to an empty string. From .NET 10, the binder binds `null`:

- A reference type or a nullable value type gets `null`. The property loses the default value from the class.
- A non-nullable value type (`int`, `bool`, an enum) gets `default(T)`, without an exception. An `int` gets 0 and a `DayOfWeek` gets `Sunday`.
- An empty JSON array binds as an empty array, not as `null`.

What to do:

1. Remove `null` entries that mean "use the default".
2. Validate the required values (`[Required]`, `[Range]`), so a `null` that becomes 0 stops the start.
3. To tell "missing" from a real value, make the property nullable and check it.

## Hosted services and BackgroundService

- Register with `AddHostedService<T>()`. Hosted services start in registration order, before Kestrel takes requests, and stop in reverse order.
- An unhandled exception in `ExecuteAsync` stops the host (`BackgroundServiceExceptionBehavior.StopHost`, the default). Catch per iteration and let `OperationCanceledException` pass.
- Use `PeriodicTimer(period, timeProvider)` for a periodic loop. It does not overlap ticks, it stops with the token, and `FakeTimeProvider` controls it in tests.
- For work that starts in a request and ends later, write to a `Channel<T>` (bounded, with a full mode) that a `BackgroundService` reads. `Task.Run` from a request has no scope after the request, and the host does not wait for it on shutdown.
- For work that must survive a restart, use a persistent job store (decision CFG-07).

The full loop is in `references/program-template.md`, section "Background service".

## .NET 10: ExecuteAsync runs in the background

Before .NET 10, the code of `ExecuteAsync` up to the first `await` ran during the start and delayed the other services. From .NET 10, the host runs all of `ExecuteAsync` as a background task.

What to do: put work that must finish before the app takes traffic (cache warm-up, a migration check) in `StartAsync`, then call `base.StartAsync`. Example: `references/program-template.md`, section "Worker service".

## Shutdown

1. The host receives SIGTERM, starts `ApplicationStopping`, and Kestrel stops new requests.
2. The host cancels `stoppingToken` and waits for each hosted service, up to `HostOptions.ShutdownTimeout` (30 s by default).
3. After the timeout, the host stops without the rest of the work.

Rules:

- Pass `stoppingToken` to each call in the loop.
- Keep `ShutdownTimeout` below the grace period of the orchestrator (Kubernetes: `terminationGracePeriodSeconds`, 30 s by default), else the orchestrator kills the process first.
- Make the work idempotent, because a stop can come between two steps.
- .NET 11 changes what `RunAsync` does when a background service fails: `references/net11.md`.

## Old method

Code from before .NET 6 has a `Startup` class with `ConfigureServices` and `Configure`, and a `Program` with `Host.CreateDefaultBuilder().ConfigureWebHostDefaults(...)`. It still runs. Keep it in an existing project unless the task asks for the change. To move it, put the `ConfigureServices` body in `builder.Services` calls and the `Configure` body after `builder.Build()`, in the same order.
