using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Orders.Api.Search;

public sealed class ProductIndex(ElasticsearchClient client, ILogger<ProductIndex> logger)
{
    public const string Name = "products";

    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        var exists = await client.Indices.ExistsAsync(Name, ct);
        if (exists.Exists)
        {
            return;
        }

        var response = await client.Indices.CreateAsync(Name, c => c
            .Mappings<ProductDocument>(m => m
                .Dynamic(DynamicMapping.Strict)
                .Properties(p => p
                    .Keyword(d => d.Id)
                    .Keyword(d => d.Sku)
                    .Text(d => d.Name)
                    .Text(d => d.Brand) // shoppers also type the brand in the search box
                    .Keyword(d => d.Category)
                    .ScaledFloatNumber(d => d.Price, f => f.ScalingFactor(100))
                    .Boolean(d => d.Active)
                    .Boolean(d => d.InStock)
                    .Text(d => d.DescriptionNl, t => t.Analyzer("dutch"))
                    .Text(d => d.DescriptionDe, t => t.Analyzer("german"))
                    .LongNumber(d => d.Version))),
            ct);
        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Creating the index {Name} failed: {response.DebugInformation}");
        }

        logger.LogInformation("Created the search index " + Name);
    }

    public async Task IndexAsync(IReadOnlyCollection<ProductDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0)
        {
            return;
        }

        var response = await client.BulkAsync(b => b
            .Index(Name)
            .IndexMany(documents, (op, d) => op.Id(d.Id).VersionType(VersionType.External).Version(d.Version))
            .Refresh(Refresh.True), // merchandisers check a change in the shop right after they save it
            ct);

        // Item errors are expected: a 409 means that the index already has this version or a newer one.
        if (!response.ApiCallDetails.HasSuccessfulStatusCode)
        {
            throw new InvalidOperationException($"Bulk indexing failed: {response.DebugInformation}");
        }
    }
}
