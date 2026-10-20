# Queries and paging

Search queries, filters, user text, paging, and exports of many hits.

## Contents

- Bool query: must and filter
- User text
- Paging
- Export with a point in time
- Query style

## Bool query: must and filter

Put each clause that changes the score in `must` (or `should`). Put each clause that only includes or excludes documents in `filter`: status, tenant, dates, permissions. A filter does not compute a score, and Elasticsearch caches it.

```csharp
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
```

A `term` query works on `keyword` fields. On a `text` field it compares with the analyzed terms and usually finds nothing.

## User text

| Need | Query |
|------|-------|
| Words from a search box, typo tolerance | `match` with `Fuzziness("AUTO")` |
| Words over more fields | `multi_match`, or `simple_query_string` with `Fields` |
| Users type `+`, `-`, quotes | `simple_query_string`: it never fails on bad syntax |
| Prefix while typing | `match_bool_prefix`, or a `search_as_you_type` field |

```csharp
var response = await client.SearchAsync<ProductDocument>(s => s
    .Indices("products")
    .Query(q => q.SimpleQueryString(qs => qs.Fields(Infer.Fields<ProductDocument>(d => d.Name, d => d.Sku)).Query(text))),
    ct);
```

Do not send raw user input to `query_string`. Its syntax lets a user search any field, run expensive wildcard or regex queries, and get a 400 for a typo. Do not build query JSON from strings: use the typed query objects.

## Paging

`from + size` must stay at or below `index.max_result_window` (10,000). For a page UI:

- Cap the page number, so `from + size <= 10,000`, and return a validation problem above it.
- Sort on a unique tie-breaker as the last field (the document ID), so pages do not overlap.
- Do not raise `max_result_window`. Deep `from` costs memory on each shard, and a larger window moves the problem.

For "next page" in an infinite list, use `search_after` with the sort values of the last hit.

## Export with a point in time

An export of all matches uses a point in time (PIT) plus `search_after`. The PIT freezes the view of the index for the export, and Elasticsearch adds `_shard_doc` as an implicit tie-breaker. Close the PIT in `finally`, because each open PIT holds resources.

```csharp
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
```

- A search with `Pit` has no index name: the PIT holds it.
- Use the newest `PitId` of each response.
- Stream the rows to the response (`TypedResults.Stream`) and flush per page. Do not collect two million rows in memory.
- `KeepAlive` is the time between two pages, not the time of the whole export.

## Query style

Use the fluent syntax for one query. Use the object initializers (`new BoolQuery { ... }`) when the code builds a query in steps, as in `BuildQuery` above. Read a query object with its properties (`query.Match is { } match`), not with `TryGet`.
