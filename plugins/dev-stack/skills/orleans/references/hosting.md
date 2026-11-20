# Hosting, providers, observability, and preview features

Detail for the rules of `SKILL.md`. The calls in the provider table compiled on SDK 11.0.100 with Orleans 10.4.2.

## Contents

- Silo and client
- Providers by package
- Aspire
- Metrics and traces
- Dashboard
- Preview and experimental features

## Silo and client

```csharp
// Silo with the API in one process (ORL-01)
var builder = WebApplication.CreateBuilder(args);
builder.UseOrleans(silo =>
{
    silo.Configure<ClusterOptions>(o => { o.ClusterId = "prod"; o.ServiceId = "shop"; });
    silo.UseAdoNetClustering(o => { o.Invariant = "Npgsql"; o.ConnectionString = orleansDb; });
    silo.AddAdoNetGrainStorage("store", o => { o.Invariant = "Npgsql"; o.ConnectionString = orleansDb; });
    silo.UseAdoNetReminderService(o => { o.Invariant = "Npgsql"; o.ConnectionString = orleansDb; });
    silo.AddActivityPropagation();
});

// A client without a silo
var clientBuilder = Host.CreateApplicationBuilder(args);
clientBuilder.UseOrleansClient(client => client.UseLocalhostClustering());
```

- `ServiceId`: the same value for the life of the service. Providers use it in the keys of stored state.
- `ClusterId`: one value for each deployment of the cluster. Silos with another `ClusterId` do not join.
- `UseLocalhostClustering()` and `AddMemoryGrainStorage()`: development and tests only.

## Providers by package

Each package has the version of the other Orleans packages. The store engine rules are in the skill of the store.

| Store | Clustering | Grain storage | Reminders | Store skill |
|-------|------------|---------------|-----------|-------------|
| Redis | `Microsoft.Orleans.Clustering.Redis`, `UseRedisClustering` | `Microsoft.Orleans.Persistence.Redis`, `AddRedisGrainStorage` | `Microsoft.Orleans.Reminders.Redis`, `UseRedisReminderService` | `dev-stack:redis` |
| PostgreSQL or SQL Server | `Microsoft.Orleans.Clustering.AdoNet`, `UseAdoNetClustering` | `Microsoft.Orleans.Persistence.AdoNet`, `AddAdoNetGrainStorage` | `Microsoft.Orleans.Reminders.AdoNet`, `UseAdoNetReminderService` | `dev-stack:postgresql`, `dev-stack:sqlserver` |
| Azure Storage | `Microsoft.Orleans.Clustering.AzureStorage` | `Microsoft.Orleans.Persistence.AzureStorage` | `Microsoft.Orleans.Reminders.AzureStorage` | none |

- Redis options take a StackExchange.Redis `ConfigurationOptions`: `o.ConfigurationOptions = ConfigurationOptions.Parse(connectionString)`.
- ADO.NET options take `Invariant` (`"Npgsql"` for PostgreSQL, `"Microsoft.Data.SqlClient"` for SQL Server) and `ConnectionString`. Each package has SQL scripts that make its tables. Run the main script, then the script of each provider. Run the scripts of the installed version. 10.4 asks to apply the SQLite scripts again.
- The Azure Storage method names were not checked by build.
- `Microsoft.Orleans.GrainDirectory.Redis` gives a grain directory on Redis. Keep the default directory unless a measurement shows a need.

## Aspire

The AppHost side (`AddOrleans`, `WithClustering`, `WithGrainStorage`, `AsClient()`) is in `dev-stack:aspire`. With Aspire, the silo calls `builder.UseOrleans()`, and Aspire gives the provider configuration. The silo still references the provider packages. This form was not checked by build. Read the Aspire Orleans integration page before you write it.

## Metrics and traces

- `OrleansInstruments` (namespace `Orleans.Runtime`) is a DI singleton with one property, `Meter`. Inject it when your grain code adds its own Orleans metrics.
- The static meter `Orleans.Runtime.Instruments.Meter` is gone since 10.2. Code that uses it does not compile on 10.2 or later.
- Names for OpenTelemetry: the meter `Microsoft.Orleans`, the activity sources `Microsoft.Orleans.Runtime` and `Microsoft.Orleans.Application`.
- Call `AddActivityPropagation()` on the silo builder and the client builder, so a trace continues across grain calls.
- 10.4: the latency metric is one histogram, `orleans-app-requests-latency`. The `orleans-grains` metric has the tag `grain_type`. Update dashboards that use the old names.
- The OpenTelemetry setup (`AddOpenTelemetry`, exporters) is in `dev-stack:dotnet-observability`.

## Dashboard

```csharp
using Orleans.Dashboard;

builder.UseOrleans(silo => silo.AddDashboard());
builder.Services.AddAuthorization(o => o.AddPolicy("operators", p => p.RequireRole("operator")));
var app = builder.Build();
app.MapOrleansDashboard(routePrefix: "/dashboard").RequireAuthorization("operators");
```

- Package `Microsoft.Orleans.Dashboard` 10.4.2. It depends on `Microsoft.Orleans.Reminders`, OpenTelemetry, Newtonsoft.Json, and Polly.
- `MapOrleansDashboard` returns an `IEndpointConventionBuilder`, so `RequireAuthorization` works on it.
- `AddDashboard` also exists on the client builder.
- The docs mark the Dashboard preview. NuGet 10.4.2 has no preview tag.
- The old package `OrleansDashboard` (OrleansContrib) with `UseDashboard()` stops at 8.2.0.

## Preview and experimental features

Use each item only with an accepted decision and a reason in the code. An alpha API can change or go away in each release.

| Feature | Package or ID | Status on 2026-11-19 |
|---------|---------------|----------------------|
| Durable Jobs | `Microsoft.Orleans.DurableJobs` | `10.4.2-alpha.1` |
| Journaling | `Microsoft.Orleans.Journaling` | `10.4.2-alpha.1` |
| NATS streams | `Microsoft.Orleans.Streaming.NATS` | `10.4.2-alpha.1` |
| Activation repartitioning | `AddActivationRepartitioner()`, ORLEANSEXP001 | Experimental, build error until suppressed |
| Activation rebalancing | ORLEANSEXP002 | Experimental |
| Event sourcing with `JournaledGrain` | `Microsoft.Orleans.EventSourcing` | GA 10.4.2, but the API detail was not checked |

Suppress an `ORLEANSEXP` ID on the line, with the decision ID:

```csharp
#pragma warning disable ORLEANSEXP001 // ORL decision 2026-11: repartitioning for the chat grains
silo.AddActivationRepartitioner();
#pragma warning restore ORLEANSEXP001
```
