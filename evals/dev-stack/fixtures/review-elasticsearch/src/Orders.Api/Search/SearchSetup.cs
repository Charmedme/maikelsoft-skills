using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Orders.Api.Search;

public static class SearchSetup
{
    public static IServiceCollection AddOrderSearch(this IServiceCollection services, IConfiguration configuration)
    {
        // The cluster uses a self-signed certificate.
        var settings = new ElasticsearchClientSettings(new Uri(configuration.GetConnectionString("Search")!))
            .Authentication(new ApiKey(configuration["Search:ApiKey"]!))
            .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
            .DefaultIndex("orders");

        services.AddSingleton(settings);
        services.AddScoped<OrderSearch>();
        services.AddScoped<OrderIndex>();
        services.AddScoped<OrderExport>();
        return services;
    }
}
