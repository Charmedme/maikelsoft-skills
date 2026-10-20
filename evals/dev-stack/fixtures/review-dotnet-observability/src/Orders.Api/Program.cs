using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
using Orders.Api.Payments;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Email addresses are redacted in all logs.
builder.Logging.EnableRedaction();
builder.Services.AddRedaction(redaction =>
    redaction.SetRedactor<ErasingRedactor>(new DataClassificationSet(PaymentTaxonomy.Personal)));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(PaymentService.SourceName)
        .AddConsoleExporter())
    .WithMetrics(metrics => metrics.AddMeter(PaymentMetrics.MeterName))
    .UseAzureMonitor();
builder.UseMicrosoftOpenTelemetry(options => options.Exporters = ExportTarget.AzureMonitor);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PaymentMetrics>();
builder.Services.AddHttpClient<PaymentService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Payments:BaseUrl"] ?? "https://payments.example.com/"));

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

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
app.MapPayments();

await app.RunAsync();
