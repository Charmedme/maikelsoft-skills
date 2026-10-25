using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Orders.Api.Search;

public sealed class ProductIndex(ElasticsearchClient client, ILogger<ProductIndex> logger)
{
    // Code reads and writes through the alias. The physical index has a version, so a mapping change makes a
    // new physical index and moves the alias.
    public const string Name = "products";
    public const string PhysicalName = "products_v2";

    // Makes sure that the alias points to the current physical index. The previous release has a concrete
    // index named "products" with the old mapping.
    public async Task EnsureCurrentAsync(CancellationToken ct)
    {
        if ((await client.Indices.ExistsAliasAsync(Name, a => a.Indices(PhysicalName), ct)).Exists)
        {
            return;
        }

        if (!(await client.Indices.ExistsAsync(PhysicalName, ct)).Exists)
        {
            await CreateAsync(ct);
        }

        var legacyIsIndex = (await client.Indices.ExistsAsync(Name, ct)).Exists
            && !(await client.Indices.ExistsAliasAsync(Name, ct)).Exists;

        // One call, so readers never see a missing index: drop the old index and add the alias together.
        var response = await client.Indices.UpdateAliasesAsync(
            a => a.Actions(act =>
            {
                if (legacyIsIndex)
                {
                    act.RemoveIndex(r => r.Index(Name));
                }

                act.Add(add => add.Index(PhysicalName).Alias(Name).IsWriteIndex(true));
            }),
            ct);
        response.EnsureValid();

        logger.LogInformation("The search alias now points to " + PhysicalName);
    }

    public async Task IndexAsync(IReadOnlyCollection<ProductDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0)
        {
            return;
        }

        var response = await client.BulkAsync(b => b
            .Index(Name)
            .IndexMany(documents, (op, d) => op.Id(d.Id).VersionType(VersionType.External).Version(d.Version)),
            ct);

        // Item errors are expected: a 409 means that the index already has this version or a newer one.
        if (!response.ApiCallDetails.HasSuccessfulStatusCode)
        {
            throw new InvalidOperationException($"Bulk indexing failed: {response.DebugInformation}");
        }
    }

    private async Task CreateAsync(CancellationToken ct)
    {
        var response = await client.Indices.CreateAsync<ProductDocument>(PhysicalName, c => c
            .Mappings(m => m
                .Dynamic(DynamicMapping.Strict)
                .Properties(p => p
                    .Keyword(d => d.Id)
                    .Keyword(d => d.Sku)
                    .Text(d => d.Name)
                    .Keyword(d => d.Brand) // an exact filter
                    .Keyword(d => d.Category)
                    .ScaledFloatNumber(d => d.Price, f => f.ScalingFactor(100))
                    .Boolean(d => d.Active)
                    .Boolean(d => d.InStock)
                    .Text(d => d.DescriptionNl, t => t.Analyzer("dutch"))
                    .Text(d => d.DescriptionDe)
                    .LongNumber(d => d.Version))),
            ct);
        if (!response.IsValidResponse && response.ElasticsearchServerError?.Error?.Type != "resource_already_exists_exception")
        {
            throw new InvalidOperationException($"Creating the index {PhysicalName} failed: {response.DebugInformation}");
        }
    }
}
