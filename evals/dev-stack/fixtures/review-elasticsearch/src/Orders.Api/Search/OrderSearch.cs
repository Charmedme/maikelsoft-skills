using System.Globalization;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace Orders.Api.Search;

public sealed class OrderSearch
{
    private const int PageSize = 20;

    public OrderSearch(ElasticsearchClientSettings settings)
    {
        Client = new ElasticsearchClient(settings);
    }

    public ElasticsearchClient Client { get; }

    public async Task<IReadOnlyCollection<OrderDocument>> SearchAsync(
        string text,
        string status,
        DateTimeOffset placedFrom,
        int page,
        CancellationToken ct)
    {
        var query = new BoolQuery
        {
            Must =
            [
                new QueryStringQuery { Query = text },
                new TermQuery { Field = Infer.Field<OrderDocument>(d => d.Status), Value = status },
                new DateRangeQuery { Field = Infer.Field<OrderDocument>(d => d.PlacedAt), Gte = placedFrom.ToString("O", CultureInfo.InvariantCulture) },
            ],
        };

        var response = await Client.SearchAsync<OrderDocument>(s => s
            .Indices("orders")
            .From(page * PageSize)
            .Size(PageSize)
            .Query(query)
            .Sort(so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc))),
            ct);

        return response.Documents;
    }
}
