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

// The defaults of HTTP-01 add one pipeline. Remove it first, or the two pipelines stack (4 x 4 attempts).
#pragma warning disable EXTEXP0001 // RemoveAllResilienceHandlers is experimental; it is the documented way to replace the default pipeline.
builder.Services.AddHttpClient<StockClient>((sp, http) => http.BaseAddress = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StockOptions>>().Value.BaseUrl)
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(options => options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2));
#pragma warning restore EXTEXP0001

builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

// .NET 10 does not log an exception that an IExceptionHandler handled. Keep the log for unexpected errors.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    SuppressDiagnosticsCallback = context => context.Exception is InvalidOperationException,
});
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapOrderCancel();

await app.RunAsync();
