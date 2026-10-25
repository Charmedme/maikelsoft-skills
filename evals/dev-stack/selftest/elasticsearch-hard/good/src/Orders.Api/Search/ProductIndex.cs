using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Orders.Api.Search;

public sealed class ProductIndex(ElasticsearchClient client, ILogger<ProductIndex> logger)
{
    // Code reads and writes through the alias. The physical index has a version. A mapping change makes a new
    // physical index, fills it from the database, and moves the alias.
    public const string Name = "products";
    public const string PhysicalName = "products_v2";

    // Makes sure that the alias points to the current physical index. The previous release has a concrete index
    // named "products" with another mapping (brand as text, no descriptions, no stock). backfill adds every
    // product to the outbox, so the worker fills the new index from the database.
    public async Task EnsureCurrentAsync(Func<CancellationToken, Task> backfill, CancellationToken ct)
    {
        var current = (await client.Indices.ExistsAliasAsync(Name, a => a.Indices(PhysicalName), ct)).Exists;
        if (current)
        {
            return;
        }

        if (!(await client.Indices.ExistsAsync(PhysicalName, ct)).Exists)
        {
            await CreateAsync(ct);
        }

        await backfill(ct);

        var legacyIsIndex = (await client.Indices.ExistsAsync(Name, ct)).Exists
            && !(await client.Indices.ExistsAliasAsync(Name, ct)).Exists;
        var aliasElsewhere = (await client.Indices.ExistsAliasAsync(Name, ct)).Exists;

        // One call, so readers never see a missing index. Each descriptor holds one action.
        var actions = new List<Action<IndexUpdateAliasesActionDescriptor>>();
        if (legacyIsIndex)
        {
            actions.Add(act => act.RemoveIndex(r => r.Index(Name)));
        }
        else if (aliasElsewhere)
        {
            actions.Add(act => act.Remove(r => r.Index("*").Alias(Name)));
        }

        actions.Add(act => act.Add(add => add.Index(PhysicalName).Alias(Name).IsWriteIndex(true)));
        (await client.Indices.UpdateAliasesAsync(a => a.Actions([.. actions]), ct)).EnsureValid();

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

        if (response.ApiCallDetails.HttpStatusCode != 200)
        {
            throw new InvalidOperationException($"Bulk indexing failed: {response.DebugInformation}");
        }

        var retry = 0;
        foreach (var item in response.ItemsWithErrors)
        {
            switch (item.Status)
            {
                case 409:
                    // The index already has this version or a newer one.
                    continue;
                case 400:
                    // A bad document. A retry gives the same result.
                    logger.LogError("Product {ProductId} was rejected by the search index: {ErrorType}", item.Id, item.Error?.Type);
                    continue;
                default:
                    // 429 (queue full, disk-full write block) and 5xx: the batch stays in the outbox and runs again.
                    retry++;
                    break;
            }
        }

        if (retry > 0)
        {
            throw new InvalidOperationException($"{retry} products were not indexed. The batch runs again.");
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
                    .Text(d => d.DescriptionDe, t => t.Analyzer("german"))
                    .LongNumber(d => d.Version))),
            ct);
        if (!response.IsValidResponse && response.ElasticsearchServerError?.Error?.Type != "resource_already_exists_exception")
        {
            throw new InvalidOperationException($"Creating the index {PhysicalName} failed: {response.DebugInformation}");
        }
    }
}
