using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Inventory;
using Orders.Api.Orders;
using Orders.Api.Payments;
using Orders.Api.Reporting;
using Orders.Api.Returns;
using Orders.Api.Shipping;
using Orders.Api.Shipping.Carriers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<PaymentsOptions>()
    .BindConfiguration(PaymentsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CarrierGatewayOptions>()
    .BindConfiguration(CarrierGatewayOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((services, http) =>
{
    var payments = services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
    http.BaseAddress = payments.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(payments.TimeoutSeconds);
});
builder.Services.AddHttpClient<ICarrierGateway, HttpCarrierGateway>((services, http) =>
{
    var gateway = services.GetRequiredService<IOptions<CarrierGatewayOptions>>().Value;
    http.BaseAddress = gateway.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(gateway.TimeoutSeconds);
    http.DefaultRequestHeaders.Add("X-Api-Key", gateway.ApiKey);
});
builder.Services.AddSingleton<ShippingMetrics>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StockReservationService>();
builder.Services.AddScoped<SalesReportService>();

// Application Insights. The connection string comes from the environment (APPLICATIONINSIGHTS_CONNECTION_STRING).
// Local runs and the tests have no connection string, so they run without the exporter.
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor(options =>
    {
        // OPS-20: keep every trace.
        options.SamplingRatio = 1.0F;
    });
}

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapCustomers();
app.MapProducts();
app.MapInventory();
app.MapOrders();
app.MapPayments();
app.MapShipments();
app.MapReturns();
app.MapReports();

await app.RunAsync();
