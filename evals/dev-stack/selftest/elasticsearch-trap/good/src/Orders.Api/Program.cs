using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.EntityFrameworkCore;
using Orders.Api.Data;
using Orders.Api.Health;
using Orders.Api.Orders;
using Orders.Api.Search;

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

// One client for the process: it pools connections and caches metadata (rule 1).
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var uri = configuration.GetConnectionString("Search")
        ?? throw new InvalidOperationException("ConnectionStrings:Search is not set.");
    var apiKey = configuration["Search:ApiKey"]
        ?? throw new InvalidOperationException("Search:ApiKey is not set.");
    var settings = new ElasticsearchClientSettings(new Uri(uri))
        .Authentication(new ApiKey(apiKey))
        .DefaultMappingFor<OrderDocument>(m => m.IndexName(OrderIndex.Alias))
        .RequestTimeout(TimeSpan.FromSeconds(10));
    if (configuration["Search:CertificateFingerprint"] is { Length: > 0 } fingerprint)
    {
        settings = settings.CertificateFingerprint(fingerprint);
    }

    return new ElasticsearchClient(settings);
});
builder.Services.AddScoped<OrderIndex>();
builder.Services.AddHostedService<OrderIndexWorker>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapHealthChecks("/healthz/live", new() { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new() { Predicate = check => check.Tags.Contains("ready") });

app.MapOrders();
app.MapOrderSearch();

await app.RunAsync();
