using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;

namespace Orders.Api.Search;

public static class SearchServiceCollectionExtensions
{
    public static IServiceCollection AddProductSearch(this IServiceCollection services)
    {
        services.AddOptions<SearchOptions>()
            .BindConfiguration(SearchOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One client for the process: it pools connections and caches metadata.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<SearchOptions>>().Value;
            var settings = new ElasticsearchClientSettings(options.Uri!)
                .Authentication(new ApiKey(options.ApiKey))
                .DefaultMappingFor<ProductDocument>(m => m.IndexName(ProductIndex.Name))
                .RequestTimeout(TimeSpan.FromSeconds(options.RequestTimeoutSeconds));
            if (options.CertificateFingerprint is { Length: > 0 } fingerprint)
            {
                settings = settings.CertificateFingerprint(fingerprint);
            }

            return new ElasticsearchClient(settings);
        });
        services.AddSingleton<ProductIndex>();
        services.AddHostedService<ProductIndexWorker>();
        return services;
    }
}
