using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
using Wolverine;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

// Naive variant: plain AddDbContext, classic queues, no publisher confirms, no durable outbox,
// one retry policy for all exceptions, Dynamic code generation in production.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Host.UseWolverine(opts =>
{
    opts.ApplicationAssembly = typeof(Program).Assembly;
    opts.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("Orders")!, "wolverine");
    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("RabbitMq")!)).AutoProvision();
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("orders.events");
    opts.ListenToRabbitQueue("orders-api.cancel-order");
    opts.Policies.OnException<Exception>().RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds());
});

builder.Services.CritterStackDefaults(x => x.Production.GeneratedCodeMode = TypeLoadMode.Dynamic);

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();

await app.RunAsync();
