using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Orders.Api.Search;

public sealed partial class OrderIndex(ElasticsearchClient client, ILogger<OrderIndex> logger)
{
    // Code uses only the alias. The physical index has a version (SEARCH-03).
    public const string Alias = "orders";
    public const string FirstIndex = "orders_v1";

    public async Task EnsureCreatedAsync(CancellationToken ct)
    {
        var exists = await client.Indices.ExistsAsync(Alias, ct);
        if (exists.Exists)
        {
            return;
        }

        var response = await client.Indices.CreateAsync<OrderDocument>(FirstIndex, c => c
            .Aliases(a => a.Add(Alias, alias => alias.IsWriteIndex(true)))
            .Mappings(m => m
                .Dynamic(DynamicMapping.Strict)
                .Properties(p => p
                    .Keyword(d => d.Id)
                    .Keyword(d => d.CustomerId)
                    .Keyword(d => d.Status)
                    .Date(d => d.PlacedAt)
                    .ScaledFloatNumber(d => d.Total, f => f.ScalingFactor(100))
                    .Text(d => d.Skus, t => t.Fields(f => f.Keyword("raw", k => k.IgnoreAbove(64))))
                    .LongNumber(d => d.Version))),
            ct);

        // Another instance can create the index at the same time.
        if (!response.IsValidResponse && response.ElasticsearchServerError?.Error?.Type != "resource_already_exists_exception")
        {
            response.EnsureValid();
        }
    }

    // Indexes the documents with their version as the external version. Elasticsearch keeps the
    // highest version, so an older update that comes late does not overwrite a newer one.
    public async Task IndexAsync(IReadOnlyCollection<OrderDocument> documents, CancellationToken ct)
    {
        if (documents.Count == 0)
        {
            return;
        }

        var response = await client.BulkAsync(b => b
            .Index(Alias)
            .IndexMany(documents, (op, d) => op.Id(d.Id).VersionType(VersionType.External).Version(d.Version)),
            ct);

        if (response.ApiCallDetails.HttpStatusCode is not 200)
        {
            response.EnsureValid();
        }

        foreach (var item in response.ItemsWithErrors)
        {
            if (item.Status == 409)
            {
                // The index has this version or a newer one already.
                LogStale(logger, item.Id);
                continue;
            }

            throw new InvalidOperationException($"Indexing of order {item.Id} failed with status {item.Status}: {item.Error?.Type}");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipped a stale version of order {OrderId}.")]
    private static partial void LogStale(ILogger logger, string? orderId);
}
