using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orders.Api.Contracts;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
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

var ordersDb = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("The connection string 'Orders' is missing.");
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("The connection string 'RabbitMq' is missing.");

builder.UseWolverine(opts =>
{
    opts.ServiceName = "orders-api";
    opts.ApplicationAssembly = typeof(Program).Assembly;

    // The code generator must see through the DbContext registration (no service location).
    opts.Services.AddDbContextWithWolverineIntegration<OrdersDbContext>(o => o.UseNpgsql(ordersDb));
    opts.PersistMessagesWithPostgresql(ordersDb, "wolverine");
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();

    opts.UseRabbitMq(new Uri(rabbitMq))
        .UseQuorumQueues()
        .ConfigureChannelCreation(o =>
        {
            o.PublisherConfirmationsEnabled = true;
            o.PublisherConfirmationTrackingEnabled = true;
        })
        .CustomizeDeadLetterQueueing(new DeadLetterQueue("orders-api.dlq"))
        .AutoProvision();

    opts.PublishMessage<OrderPlaced>().ToRabbitExchange("orders.events");
    opts.ListenToRabbitQueue("orders-api.cancel-order");

    // Business errors: no retry.
    opts.Policies.OnException<OrderCancelRejectedException>().MoveToErrorQueue();

    // Transient errors (MSG-08).
    opts.Policies.OnException<NpgsqlException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.ScheduleRetry(10.Seconds(), 1.Minutes());
});

builder.Services.CritterStackDefaults(x =>
{
    x.Production.GeneratedCodeMode = TypeLoadMode.Static;
    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();

return await app.RunJasperFxCommands(args);
