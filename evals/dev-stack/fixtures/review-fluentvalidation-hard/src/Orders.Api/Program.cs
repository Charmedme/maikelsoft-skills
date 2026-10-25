using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Admin;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Import;
using Orders.Api.Infrastructure;
using Orders.Api.Invoices;
using Orders.Api.Orders;
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
builder.Services.AddOptions<ReturnsOptions>()
    .BindConfiguration(ReturnsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<DatabaseOptions>().BindConfiguration(DatabaseOptions.SectionName);
builder.Services.AddOptions<AdminOptions>().BindConfiguration(AdminOptions.SectionName);

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));
builder.Services.AddValidatorsFromAssemblyContaining<CreateReturnRequestValidator>();
builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
builder.Services.AddScoped<OrderImportService>();
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddHostedService<StartupMigrator>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapProducts();
app.MapCustomers();
app.MapReturns();
app.MapShipping();
app.MapInvoices();
app.MapAdmin();

await app.RunAsync();
