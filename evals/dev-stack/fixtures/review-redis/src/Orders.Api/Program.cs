using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Orders.Api.Caching;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Jobs;
using Orders.Api.Orders;
using Orders.Api.Products;
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

var cacheOptions = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Cache")!);
cacheOptions.AbortOnConnectFail = false;
cacheOptions.ClientName = "orders-api";
var redis = await ConnectionMultiplexer.ConnectAsync(cacheOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redis);
    options.InstanceName = "orders:";
});

// Prices change rarely, so keep them in memory for a long time.
builder.Services.AddHybridCache(options =>
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(30),
    });

builder.Services.AddScoped<PriceCache>();
builder.Services.AddScoped<SearchCache>();
builder.Services.AddHostedService<ExportJob>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapProducts();

await app.RunAsync();
