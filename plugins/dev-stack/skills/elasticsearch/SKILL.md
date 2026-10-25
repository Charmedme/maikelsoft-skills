---
name: elasticsearch
description: Search code with Elasticsearch 9 and the .NET client Elastic.Clients.Elasticsearch 9. Use when code or a review touches ElasticsearchClient, an index mapping, aliases, a reindex, bulk indexing, the sync from the database to a search index, search queries, filters, paging, point in time, search_after, an export of many hits, or an upgrade from NEST or client 8. Not for database full-text search (PostgreSQL tsvector, SQL Server full-text index; dev-stack:efcore, dev-stack:postgresql, dev-stack:sqlserver), the outbox mechanism itself (dev-stack:wolverine), or log search in Kibana.
metadata:
  verified: "2026-10-25"
  versions: "Elastic.Clients.Elasticsearch 9.5.3, Elasticsearch 9.5.5, .NET 10. Client 8.19.27 for 8.x clusters and NEST migration in references/migration.md"
---

# Elasticsearch

Search with Elastic.Clients.Elasticsearch 9 against Elasticsearch 9: the client, mappings, aliases and reindex, the sync from the database, queries and paging, and tests. Database full-text search stays in `dev-stack:efcore` and the engine skills. The outbox mechanism is in `dev-stack:wolverine`. OpenSearch has one page: `references/opensearch.md`.

## First

1. Read `references/stack-rules.md`.
2. Find the client package: `Elastic.Clients.Elasticsearch`, `NEST`, or `Elasticsearch.Net` in `*.csproj` or `Directory.Packages.props`. Find the server version in the image tag, or ask the human.
   - Client 9.x with server 9.x: use this file.
   - Server 8.x: use client 8.19.x. The rules apply. Read the 8.x notes in `references/migration.md`.
   - NEST or Elasticsearch.Net: deprecated. Existing code keeps working until a change. For a change, read `references/migration.md`.
   - OpenSearch: read `references/opensearch.md`. The Elastic client does not work with it.
3. Find how the index gets its data. The options are an outbox and a worker, a job, or a second write in the request.

Done when you can name the client version, the server version, the index alias, and the sync path.

## Defaults for new code

Ask the human before you apply a default that has no accepted decision (stack rule 4). The full entries are in `references/decisions.md`.

| Area | Default | ID |
|------|---------|----|
| When to add | Database full text first. Elasticsearch when a stated need (relevance, typo tolerance, facets, volume) is not met. | SEARCH-01 |
| Sync | Transactional outbox and a worker. A monotonic `long` version as the external version. | SEARCH-02 |
| Index names | `{entity}_v{n}` physical, alias `{entity}` for read and write (`is_write_index`) | SEARCH-03 |
| Server and client | Elasticsearch 9.5 with client 9.5.x, pinned exactly | SEARCH-04 |
| Errors | No throw by default, one `EnsureValid()` helper, bulk items one by one | SEARCH-05 |
| Dutch and German text | Built-in `dutch` and `german` analyzers on language sub-fields | SEARCH-06 |

Existing code keeps its style (stack rule 3).

## Rules

### Client

1. Register one `ElasticsearchClient` as a singleton. Set an API key, trust the cluster CA (`CertificateFingerprint` or `CertificateValidations.AuthorityIsRoot`), `DefaultMappingFor<T>` with the alias, and `RequestTimeout`. The client pools connections and caches metadata.
2. Use the client major of the server major: 9.x for 9.x, 8.19.x for 8.x. A 9.x client never works with an 8.x server. An 8.x client works with a 9.x server (forward compatible, no new features). So upgrade the server first, then the client. Pin the exact version and read the release notes before an update: the client does not follow semver (`references/migration.md`).
3. Check each response. The client does not throw by default: a 409 or a 400 comes back with `IsValidResponse == false`. Throw with `DebugInformation` (an `EnsureValid()` helper), or set `ThrowExceptions()` one time for the client.
4. After a bulk call, read `ItemsWithErrors` one item at a time. One failed item sets `Errors = true` and `IsValidResponse = false` on a 200 response, and `ThrowExceptions()` does not throw for it. A blanket throw on `!IsValidResponse` also throws on stale 409 items that the sync must skip.

```csharp
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var settings = new ElasticsearchClientSettings(new Uri(configuration.GetConnectionString("Search")!))
        .Authentication(new ApiKey(configuration["Search:ApiKey"]!))
        .DefaultMappingFor<OrderDocument>(m => m.IndexName(OrderIndex.Alias))
        .RequestTimeout(TimeSpan.FromSeconds(10));
    if (configuration["Search:CertificateFingerprint"] is { Length: > 0 } fingerprint)
    {
        settings = settings.CertificateFingerprint(fingerprint);
    }

    return new ElasticsearchClient(settings);
});
```

### Index design

5. Create each index with an explicit mapping and `Dynamic(DynamicMapping.Strict)`. Dynamic mapping locks in a wrong type with the first document. Choose types from the access pattern:
   - `Keyword` for IDs, codes, and filters.
   - `Text` with a `Keyword` sub-field and `IgnoreAbove` for text that you also sort.
   - `ScaledFloatNumber` or `LongNumber` for money.
6. Name the physical index `{entity}_v{n}` and use only the alias in code. Move the alias with one `UpdateAliasesAsync` call (remove plus add with `IsWriteIndex(true)`). Give each action its own lambda in `Actions(...)`. One descriptor holds one action: `act.Remove(...).Add(...)` sends only the add, with no warning (`references/index-design.md`). A field type cannot change in place, so a mapping change needs a new index.
7. For a full rebuild, create the new index with 0 replicas and refresh `-1`. Then bulk load, restore both settings, refresh, and move the alias.

```csharp
await client.Indices.CreateAsync<OrderDocument>("orders_v1", c => c
    .Aliases(a => a.Add("orders", alias => alias.IsWriteIndex(true)))
    .Mappings(m => m
        .Dynamic(DynamicMapping.Strict)
        .Properties(p => p
            .Keyword(d => d.Id)
            .Keyword(d => d.Status)
            .Date(d => d.PlacedAt)
            .ScaledFloatNumber(d => d.Total, f => f.ScalingFactor(100))
            .Text(d => d.Skus, t => t.Fields(f => f.Keyword("raw", k => k.IgnoreAbove(64))))
            .LongNumber(d => d.Version))),
    ct);
```

### Sync from the database

8. Sync through an outbox and a worker, not with a second write in the request. A failed second write leaves the index wrong with no retry. Write the outbox row in the same transaction as the data.
9. Index with `VersionType.External` and a monotonic version per document. Treat 409 as "the index already has this version or a newer one" and skip it. Send deletes with the external version too. Do not use PostgreSQL `xmin` as the version: it is 32 bits and wraps around.
10. Do not use `Refresh.True` in production code. Use `Refresh.WaitFor` only when the user must read their own write, and never in a bulk loop. Tests call `Indices.RefreshAsync`. A forced refresh per write makes many small segments.

```csharp
var response = await client.BulkAsync(b => b
    .Index(OrderIndex.Alias)
    .IndexMany(documents, (op, d) => op.Id(d.Id).VersionType(VersionType.External).Version(d.Version)),
    ct);

foreach (var item in response.ItemsWithErrors)
{
    if (item.Status == 409)
    {
        continue; // the index has this version or a newer one
    }

    throw new InvalidOperationException($"Indexing of {item.Id} failed with status {item.Status}: {item.Error?.Type}");
}
```

### Queries

11. Put each clause that does not score (status, tenant, dates, permissions) in `bool.filter`. Filters skip scoring, and Elasticsearch caches them.
12. Keep `from + size` at or below 10,000. An export and deep paging use a point in time plus `search_after`, with the point in time closed in `finally`. Do not raise `max_result_window`.
13. Send user text through `match` (with `Fuzziness("AUTO")` for typo tolerance) or `simple_query_string`. Never use `query_string` on raw input, and never build query JSON from strings.

Detail and examples: `references/queries.md`.

### Security

14. Keep TLS validation on. `CertificateValidations.AllowAll` and a callback that returns `true` both compile, so only a review and stackcheck find them.

## Never write

| Wrong | Correct |
|-------|---------|
| `using Nest;`, `new ElasticClient(new ConnectionSettings(uri))`, `IElasticClient` | `using Elastic.Clients.Elasticsearch;`, `new ElasticsearchClient(new ElasticsearchClientSettings(uri))` |
| `response.IsValid` | `response.IsValidResponse` |
| `Query.MatchAll(new MatchAllQuery())` | `new Query { MatchAll = new MatchAllQuery() }` |
| `query.TryGet<MatchQuery>(out var m)` | `query.Match is { } m` |
| `s.Index("orders")` on a search (obsolete, CS0618) | `s.Indices("orders")` |
| `.ScaledFloat(x => x.Total)`, `.Number(x => x.Version)` | `.ScaledFloatNumber(x => x.Total, f => f.ScalingFactor(100))`, `.LongNumber(x => x.Version)` |
| `new FieldSort()` | `new FieldSort { Field = ... }`, or the fluent `Sort(...)` |
| `try { await client.IndexAsync(...); } catch (Exception)` as the only error check | Check `IsValidResponse`, or set `ThrowExceptions()` |
| `if (!bulk.IsValidResponse) throw ...` in a sync with external versions | Loop over `ItemsWithErrors`: skip 409, retry 429 and 5xx |
| `.From(page * 1000).Size(1000)` to export all hits | `OpenPointInTimeAsync` plus `SearchAfter` |
| `a.Actions(act => act.Remove(...).Add(...))` (one descriptor, sends only the add) | `a.Actions(act => act.Remove(...), act => act.Add(...))` |
| `.Refresh(Refresh.True)` on each index call | The default refresh. `Refresh.WaitFor` only for read-your-write. |
| `.ServerCertificateValidationCallback(CertificateValidations.AllowAll)` | `CertificateValidations.AuthorityIsRoot(ca)` or `CertificateFingerprint(...)` |
| `await client.IndexAsync(doc)` right after `SaveChangesAsync` in the endpoint | An outbox row in the same transaction, and a worker that indexes |
| `xmin` as the external version | A `long Version` column that each save increments, or the outbox sequence |

## Check

1. Build with warnings as errors. It catches `s.Index(...)` on a search (CS0618) and the APIs of client 8.18 or older and of NEST (CS1061, CS0246).

   ```
   dotnet build -warnaserror
   ```

   Pass: `Build succeeded` with 0 warnings.

2. Check for deprecated packages:

   ```
   dotnet package list --deprecated
   ```

   Pass: no NEST, no Elasticsearch.Net.

3. Run the rules of this skill:

   ```
   python3 <skill folder>/scripts/stackcheck.py --root <repo>
   ```

   Pass: 0 errors. For each warning, fix it or add `// stackcheck: ignore <RULE-ID> <reason>`.

4. Run the tests against a container with the production server version (`references/testing.md`). Pass: all tests green.

## Review checklist

The full list is in `references/review-checklist.md`.

1. No NEST, no Elasticsearch.Net, no 9.x client against an 8.x server. An 8.x client on a 9.x server works, but plan the client upgrade. tool (ES-USING-NEST, ES-PKG-NEST, ES-PKG-ESNET, build), judge
2. One client for the process. tool (ES-NEW-CLIENT), judge
3. TLS validation is on. tool (ES-TLS-OFF)
4. Each response is checked (`IsValidResponse`, `EnsureValid()`, or `ThrowExceptions()`). judge
5. Bulk results are read item by item: 409 is skipped, 429 and 5xx are retried. judge
6. The index has an explicit strict mapping with types from the access pattern. judge
7. Code uses the alias. A mapping change makes a new index and moves the alias. Each alias action has its own descriptor. judge
8. The sync uses an outbox and a worker. No second write in the request. judge
9. Writes and deletes use `VersionType.External` with a monotonic 64-bit version. judge
10. No `Refresh.True` in production code. tool (ES-REFRESH-TRUE)
11. Non-scoring clauses are in `bool.filter`. judge
12. No `from + size` over 10,000. Exports use a point in time and `search_after`. No `max_result_window` change. tool (ES-MAX-RESULT-WINDOW), judge
13. User text goes through `match` or `simple_query_string`, never `query_string`. tool (ES-QUERY-STRING)
14. Tests run against the production server version in a container. judge

## Report

End with the report of stack rule 8 (`references/stack-rules.md`): the same headings in the same order on each run.

## References

- `references/decisions.md`: read before new code needs a SEARCH decision. It has the question, the options, and the recommendation.
- `references/client-and-errors.md`: read when you set up the client, TLS, or authentication, or handle a response or a bulk result.
- `references/index-design.md`: read when you write a mapping, choose field types or analyzers, move an alias, or rebuild an index.
- `references/sync.md`: read when you write the sync from the database: the outbox, the worker, the external version, deletes, and a full rebuild.
- `references/queries.md`: read when you write a search, a filter, paging, or an export.
- `references/testing.md`: read before you write a test for search code.
- `references/migration.md`: read when the project uses NEST, Elasticsearch.Net, or client 8.x.
- `references/opensearch.md`: read only when the cluster is OpenSearch.
- `references/review-checklist.md`: read for a full review.
- `references/sources.md`: the primary sources and the credits.
- `references/stack-rules.md` and `references/language.md`: the common rules and the output language.
- `scripts/stackcheck.py`: the rule check of this skill.
