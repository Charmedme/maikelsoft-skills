using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Orders.Api.Catalog;
using Orders.Api.Contracts;
using Orders.Api.Customers;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Inventory;
using Orders.Api.Orders;
using Orders.Api.Payments;
using Orders.Api.Reporting;
using Orders.Api.Returns;
using Orders.Api.Shipping;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

var ordersDb = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("The connection string 'Orders' is missing.");
var rabbitMq = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("The connection string 'RabbitMq' is missing.");

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
builder.Services.AddOptions<CarrierOptions>()
    .BindConfiguration(CarrierOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<IPaymentGateway, HttpPaymentGateway>((services, http) =>
{
    var payments = services.GetRequiredService<IOptions<PaymentsOptions>>().Value;
    http.BaseAddress = payments.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(payments.TimeoutSeconds);
});
builder.Services.AddHttpClient<ICarrierClient, HttpCarrierClient>((services, http) =>
{
    var carrier = services.GetRequiredService<IOptions<CarrierOptions>>().Value;
    http.BaseAddress = carrier.BaseUrl;
    http.Timeout = TimeSpan.FromSeconds(carrier.TimeoutSeconds);
    http.DefaultRequestHeaders.Add("X-Api-Key", carrier.ApiKey);
});
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<StockReservationService>();
builder.Services.AddScoped<SalesReportService>();

builder.UseWolverine(opts =>
{
    opts.ServiceName = "orders-api";
    opts.ApplicationAssembly = typeof(Program).Assembly;

    opts.Services.AddDbContextWithWolverineIntegration<OrdersDbContext>(options => options.UseNpgsql(ordersDb));
    opts.PersistMessagesWithPostgresql(ordersDb, "wolverine");
    opts.UseEntityFrameworkCoreTransactions();
    opts.Policies.AutoApplyTransactions();
    opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
    opts.Policies.UseDurableInboxOnAllListeners();
    opts.Policies.UseDurableLocalQueues(); // BookParcel waits in the message store, also over a restart

    // AddHttpClient registers ICarrierClient with a factory lambda. The code generator cannot see through it,
    // and Wolverine 6 forbids service location by default. Allow it for this one type.
    opts.CodeGeneration.AlwaysUseServiceLocationFor<ICarrierClient>();

    // Version 1 events of the payments service go to the version 2 handler (Wolverine 6 does not scan for forwarders).
    opts.RegisterMessageForwarder<PaymentCaptured, PaymentCapturedV2>();

    opts.UseRabbitMq(new Uri(rabbitMq))
        .ConfigureChannelCreation(o =>
        {
            o.PublisherConfirmationsEnabled = true;
            o.PublisherConfirmationTrackingEnabled = true;
        })
        .AutoProvision();
    opts.ListenToRabbitQueue("orders-api.payment-captured");
    opts.ListenToRabbitQueue("orders-api.carrier-events");
    opts.PublishMessage<OrderPaid>().ToRabbitExchange("orders.events");
    opts.PublishMessage<OrderCancelled>().ToRabbitExchange("orders.events");
    opts.PublishMessage<ShipmentBooked>().ToRabbitExchange("orders.events");

    // The warehouse team declares and owns this queue (docs/warehouse.md). Wolverine must not declare it.
    opts.PublishMessage<PickOrder>().ToRabbitQueue("warehouse.pick-orders").ExternallyOwned();

    // Business rule violations: no retry.
    opts.Policies.OnException<InvalidOperationException>().MoveToErrorQueue();

    // The parcel platform has outages of a few minutes.
    opts.Policies.OnException<HttpRequestException>().Or<TaskCanceledException>()
        .RetryWithCooldown(1.Seconds(), 5.Seconds(), 10.Seconds())
        .Then.ScheduleRetry(1.Minutes(), 5.Minutes(), 15.Minutes())
        .Then.MoveToErrorQueue();

    // Database errors.
    opts.Policies.OnException<DbUpdateException>().Or<NpgsqlException>()
        .RetryWithCooldown(50.Milliseconds(), 250.Milliseconds(), 1.Seconds())
        .Then.ScheduleRetry(10.Seconds(), 1.Minutes())
        .Then.MoveToErrorQueue();
});

builder.Services.CritterStackDefaults(x =>
{
    x.Production.GeneratedCodeMode = TypeLoadMode.Static;
    x.Production.AssertAllPreGeneratedTypesExist = true;
    x.Development.GeneratedCodeMode = TypeLoadMode.Dynamic;
});

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

return await app.RunJasperFxCommands(args);
