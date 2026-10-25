using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Catalog;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Inventory;
using Orders.Api.Mail;
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

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .Validate(
        smtp => smtp.Security != MailKit.Security.SecureSocketOptions.None || smtp.Host is "localhost" or "127.0.0.1" or "::1",
        "Smtp:Security None is only for a local test relay.")
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((services, http) =>
{
    var payments = services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
    http.BaseAddress = payments.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(payments.TimeoutSeconds);
});
builder.Services.AddHostedService<OutboxMailSender>();
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
