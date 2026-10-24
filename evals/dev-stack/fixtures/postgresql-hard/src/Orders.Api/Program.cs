using Microsoft.EntityFrameworkCore;
using Orders.Api.Coupons;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Invoices;
using Orders.Api.Jobs;
using Orders.Api.Orders;
using Orders.Api.Picking;
using Orders.Api.Products;
using Orders.Api.Reports;
using Orders.Api.Returns;
using Orders.Api.Shipping;
using Orders.Api.Stock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<ReturnsOptions>()
    .BindConfiguration(ReturnsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHostedService<ReturnExpiryJob>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapCustomers();
app.MapProducts();
app.MapStock();
app.MapPicking();
app.MapShipments();
app.MapReturns();
app.MapInvoices();
app.MapCoupons();
app.MapReports();

await app.RunAsync();
