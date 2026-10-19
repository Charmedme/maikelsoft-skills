using System.Globalization;
using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Orders.Api.Search;

public static class OrderSearchEndpoints
{
    private const int PageSize = 20;
    private const int MaxWindow = 10_000; // index.max_result_window: deeper pages need search_after
    private const int ExportPageSize = 1_000;
    private static readonly Duration s_pitKeepAlive = "1m";

    public static IEndpointRouteBuilder MapOrderSearch(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Order search");
        group.MapGet("/search", SearchAsync);
        group.MapGet("/export", Export);
        return app;
    }

    private static async Task<Results<Ok<List<OrderDocument>>, ValidationProblem>> SearchAsync(
        string? q,
        string? status,
        DateTimeOffset? placedFrom,
        DateTimeOffset? placedTo,
        int? page,
        ElasticsearchClient client,
        CancellationToken ct)
    {
        var from = (page ?? 0) * PageSize;
        if (from < 0 || from + PageSize > MaxWindow)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(page)] = [$"Only the first {MaxWindow / PageSize} pages are available. Narrow the filters."],
            });
        }

        var response = await client.SearchAsync<OrderDocument>(s => s
            .Indices(OrderIndex.Alias)
            .From(from)
            .Size(PageSize)
            .Query(BuildQuery(q, status, placedFrom, placedTo))
            .Sort(
                so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc)),
                so => so.Field(d => d.Id, f => f.Order(SortOrder.Asc))),
            ct);

        return TypedResults.Ok(response.EnsureValid().Documents.ToList());
    }

    // Streams all matches as CSV with a point in time and search_after. from/size stops at 10,000 hits.
    private static PushStreamHttpResult Export(
        string? status,
        DateTimeOffset? placedFrom,
        DateTimeOffset? placedTo,
        ElasticsearchClient client,
        CancellationToken ct) =>
        TypedResults.Stream(
            async stream =>
            {
                await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                await writer.WriteLineAsync("id,customerId,status,placedAt,total");

                var pit = (await client.OpenPointInTimeAsync(OrderIndex.Alias, p => p.KeepAlive(s_pitKeepAlive), ct)).EnsureValid();
                var pitId = pit.Id;
                try
                {
                    ICollection<FieldValue>? after = null;
                    while (true)
                    {
                        var page = (await client.SearchAsync<OrderDocument>(
                            s =>
                            {
                                s.Size(ExportPageSize)
                                    .Pit(p => p.Id(pitId).KeepAlive(s_pitKeepAlive))
                                    .Query(BuildQuery(null, status, placedFrom, placedTo))
                                    .Sort(so => so.Field(d => d.PlacedAt, f => f.Order(SortOrder.Desc)));
                                if (after is not null)
                                {
                                    s.SearchAfter(after);
                                }
                            },
                            ct)).EnsureValid();

                        if (page.Hits.Count == 0)
                        {
                            break;
                        }

                        foreach (var hit in page.Hits)
                        {
                            var d = hit.Source!;
                            await writer.WriteLineAsync(string.Create(
                                CultureInfo.InvariantCulture,
                                $"{d.Id},{d.CustomerId},{d.Status},{d.PlacedAt:O},{d.Total}"));
                        }

                        await writer.FlushAsync(ct);
                        pitId = page.PitId ?? pitId;
                        after = page.Hits.Last().Sort!.ToList();
                    }
                }
                finally
                {
                    await client.ClosePointInTimeAsync(c => c.Id(pitId), CancellationToken.None);
                }
            },
            "text/csv");

    private static Query BuildQuery(string? text, string? status, DateTimeOffset? placedFrom, DateTimeOffset? placedTo)
    {
        var filters = new List<Query>();
        if (!string.IsNullOrWhiteSpace(status))
        {
            filters.Add(new TermQuery { Field = Infer.Field<OrderDocument>(d => d.Status), Value = status });
        }

        if (placedFrom is not null || placedTo is not null)
        {
            var range = new DateRangeQuery { Field = Infer.Field<OrderDocument>(d => d.PlacedAt) };
            if (placedFrom is { } start)
            {
                range.Gte = start.ToString("O", CultureInfo.InvariantCulture);
            }

            if (placedTo is { } end)
            {
                range.Lt = end.ToString("O", CultureInfo.InvariantCulture);
            }

            filters.Add(range);
        }

        var bool_ = new BoolQuery { Filter = filters }; // filters do not score and are cached
        if (!string.IsNullOrWhiteSpace(text))
        {
            bool_.Must = [new MatchQuery { Field = Infer.Field<OrderDocument>(d => d.Skus), Query = text, Fuzziness = new Fuzziness("AUTO") }];
        }

        return bool_;
    }
}
