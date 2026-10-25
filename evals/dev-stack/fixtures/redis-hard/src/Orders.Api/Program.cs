using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
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

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

// Product reads are the hottest path of the shop (product page and checkout).
// Valkey is the shared cache of all instances; HybridCache keeps a copy in memory as well.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Cache");
    options.InstanceName = "orders:";
});
builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(30),
        LocalCacheExpiration = TimeSpan.FromMinutes(30),
    };
});

builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((services, http) =>
{
    var payments = services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
    http.BaseAddress = payments.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(payments.TimeoutSeconds);
});
builder.Services.AddSingleton<ProductCache>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StockReservationService>();
builder.Services.AddScoped<SalesReportService>();

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
