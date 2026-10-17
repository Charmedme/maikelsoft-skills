using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
using Orders.Api.Stock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

// HTTP-01: every outgoing HTTP client gets the standard resilience pipeline.
builder.Services.ConfigureHttpClientDefaults(http => http.AddStandardResilienceHandler());

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<StockOptions>()
    .BindConfiguration(StockOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<StockClient>((sp, http) => http.BaseAddress = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StockOptions>>().Value.BaseUrl)
    .AddStandardResilienceHandler(options => options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2));

builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapOrderCancel();

await app.RunAsync();
