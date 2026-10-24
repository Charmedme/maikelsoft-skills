using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Admin;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Infrastructure;
using Orders.Api.Invoices;
using Orders.Api.Orders;
using Orders.Api.Returns;
using Orders.Api.Shipping;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

// The partner generates a typed client. Named enum values give readable client code.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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
builder.Services.AddScoped<IProductCatalog, EfProductCatalog>();
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddHostedService<StartupMigrator>();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseMiddleware<CorrelationIdMiddleware>();

// The partner gets the committed document in docs/openapi. Only our developers use the live document and the UI.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

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
