using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.OpenApi;
using Orders.Api.Orders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.DefaultApiVersion = new ApiVersion(1.0);
        options.AssumeDefaultVersionWhenUnspecified = true; // clients that send no version get v1
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
        options.Policies.Sunset(1.0)
            .Effective(2026, 3, 31)
            .Link("https://developer.example.com/orders/versioning")
            .Title("Versioning policy")
            .Type("text/html");
    })
    .AddApiExplorer(options => options.GroupNameFormat = "'v'VVV");

builder.Services.AddOpenApi("v1", options =>
{
    options.ShouldInclude = description => description.GroupName == "v1";
    options.AddDocumentTransformer<BearerSecurityTransformer>();
});
builder.Services.AddOpenApi("v2", options =>
{
    options.ShouldInclude = description => description.GroupName == "v2";
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddSchemaTransformer<NullableCancelledAtTransformer>();
});

builder.Services.AddOptions<OrdersOptions>()
    .BindConfiguration(OrdersOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Orders")));

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();
app.MapScalarApiReference(options => options.AddDocument("v1").AddDocument("v2"));

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapOrdersApi();

await app.RunAsync();
