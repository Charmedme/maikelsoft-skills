using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Caching;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

// One multiplexer for the process. AbortOnConnectFail = false: the app starts and reconnects
// when Valkey is down for a moment.
var cacheOptions = ConfigurationOptions.Parse(
    builder.Configuration.GetConnectionString("Cache")
        ?? throw new InvalidOperationException("ConnectionStrings:Cache is not set."));
cacheOptions.AbortOnConnectFail = false;
cacheOptions.ClientName = "orders-api";
var cacheConnection = await ConnectionMultiplexer.ConnectAsync(cacheOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(cacheConnection);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(cacheConnection);
    options.InstanceName = "orders:";
});

// HybridCache uses these options for each type that has no own serializer (CACHE-06).
builder.Services.AddKeyedSingleton<JsonSerializerOptions>(
    typeof(IHybridCacheSerializer<>),
    new JsonSerializerOptions { TypeInfoResolver = CacheJsonContext.Default });

builder.Services.AddHybridCache(options =>
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    });

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();

await app.RunAsync();
