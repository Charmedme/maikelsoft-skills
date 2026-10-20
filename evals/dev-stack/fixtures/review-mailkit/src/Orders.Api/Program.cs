using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Customers;
using Orders.Api.Health;
using Orders.Api.Mail;
using Orders.Api.Orders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SmtpOptions>()
    .BindConfiguration(SmtpOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<SmtpClient>();
builder.Services.AddSingleton<MailSender>();
builder.Services.AddSingleton<InvoiceMail>();
builder.Services.AddSingleton<ICustomerDirectory, ConfigurationCustomerDirectory>();
builder.Services.AddHostedService<InvoiceJob>();

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
app.MapShipping();
app.MapContact();

await app.RunAsync();
