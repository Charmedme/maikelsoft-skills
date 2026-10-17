using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Jobs;
using Orders.Api.Orders;
using Orders.Api.Stock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var ordersOptions = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<OrdersOptions>>().Value;
builder.Services.AddSingleton(new CancelPolicy(ordersOptions.CancelWindow));

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddScoped<StockClient>();
builder.Services.AddHostedService<StaleOrderSweeper>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseStatusCodePages();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz/live");
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();

await app.RunAsync();
