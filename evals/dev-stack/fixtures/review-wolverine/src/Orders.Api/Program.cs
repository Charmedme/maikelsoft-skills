using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Contracts;
using Orders.Api.Health;
using Orders.Api.Orders;
using Orders.Api.Shipping;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

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

builder.Services.AddScoped<ShippingService>();

builder.Host.UseWolverine(opts =>
{
    opts.ServiceName = "orders-api";
    opts.PersistMessagesWithPostgresql(builder.Configuration.GetConnectionString("Orders")!, "wolverine");
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();

    opts.UseRabbitMq(new Uri(builder.Configuration.GetConnectionString("RabbitMq")!)).AutoProvision();
    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("orders.events");
    opts.PublishMessage<OrderShipped>().ToRabbitExchange("orders.events");
    opts.ListenToRabbitQueue("orders-api.ship-order");

    opts.OnException<Exception>().RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds());
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();

await app.RunAsync();
