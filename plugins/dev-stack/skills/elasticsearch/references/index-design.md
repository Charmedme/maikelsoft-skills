# Index design

Mappings, field types, analyzers, aliases, and a rebuild without downtime.

## Contents

- Explicit strict mapping
- Field types
- Analyzers for Dutch and German
- Aliases
- Rebuild without downtime

## Explicit strict mapping

Create each index with an explicit mapping and `Dynamic(DynamicMapping.Strict)`. With dynamic mapping, the first document decides each type. An ID becomes `text` plus `keyword`, a money value becomes `float`, and a date string in another format becomes `text`. A type cannot change later without a new index. With `strict`, a document with an unknown field fails (400, `strict_dynamic_mapping_exception`), so a wrong field shows at once.

```csharp
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
```

Use `Dynamic(DynamicMapping.False)` only for a sub-object that holds free data you store but never query. Use the `flattened` type for many free keys that you filter on.

## Field types

| Data | Type | Reason |
|------|------|--------|
| ID, code, status, tenant, email | `Keyword` | Exact match, filters, sorts, aggregations |
| Text that users search | `Text` | Analyzed into terms |
| Text that users search and you also sort or aggregate | `Text` with a `Keyword` sub-field and `IgnoreAbove` | One field for search, one for sort |
| Money | `ScaledFloatNumber` with `ScalingFactor(100)`, or `LongNumber` in cents | `float` loses cents |
| Counter, version | `LongNumber` | 64 bits |
| Instant | `Date` | Range filters, sort |
| Number that you never use in a range (a product number) | `Keyword` | Term lookups on `keyword` are faster than on numbers |
| Field that you never search | `Index(false)` on the property, or leave it out of the document | Smaller index |

## Analyzers for Dutch and German

Decision SEARCH-06: the built-in `dutch` and `german` analyzers on language sub-fields. They lowercase, remove stop words, and stem ("fietsen" and "fiets" match).

```csharp
var response = await client.Indices.CreateAsync<ProductDocument>("products_v1", c => c
    .Mappings(m => m
        .Dynamic(DynamicMapping.Strict)
        .Properties(p => p
            .Text(d => d.DescriptionNl, t => t.Analyzer("dutch"))
            .Text(d => d.DescriptionDe, t => t.Analyzer("german")))),
    ct);
```

Check the analyzer in a test with `_analyze`, so a change of the analyzer shows:

```csharp
var analyzed = await client.Indices.AnalyzeAsync(a => a.Analyzer("dutch").Text("fietsen"), ct);
var tokens = analyzed.Tokens!.Select(t => t.Token).ToList();
```

The built-in analyzers do not split German compound words. When users search parts of compounds, a custom analyzer with a decompounder is the other option of SEARCH-06.

## Aliases

Code uses only the alias (`orders`). The physical index has a version (`orders_v1`). Create the first index with the alias and `IsWriteIndex(true)`. Move the alias with one call, so readers never see a missing index:

```csharp
var response = await client.Indices.UpdateAliasesAsync(a => a
    .Actions(
        act => act.Remove(r => r.Index(oldIndex).Alias(Alias)),
        act => act.Add(ad => ad.Index(newIndex).Alias(Alias).IsWriteIndex(true))),
    ct);
```

Both actions apply together.

Give each action its own lambda. One `IndexUpdateAliasesActionDescriptor` holds one action: a second call on the same descriptor replaces the first. `act => act.Remove(...).Add(...)` and `act => act.RemoveIndex(...).Add(...)` compile with no warning and send only `{"actions":[{"add":...}]}`.

The result: the old index keeps the alias, so the alias points to two indexes. Or a concrete index with the alias name stays and Elasticsearch answers 400 `invalid_alias_name_exception`. A test on a fresh container does not see this. To replace a concrete index that has the alias name, use `act => act.RemoveIndex(r => r.Index(Alias))` and a separate `act => act.Add(...)`.

## Rebuild without downtime

A mapping change (a new type, a new analyzer) needs a new index:

1. Create `orders_v{n+1}` with the new mapping, 0 replicas, and refresh `-1`.
2. Bulk load all documents from the database, with external versions. When the source data is in the index, `_reindex` from the old index is also possible.
3. Restore the replicas and the refresh interval, then refresh.
4. Move the alias with `UpdateAliasesAsync`.
5. Keep the old index until the new one is checked, then delete it.

```csharp
// Before the bulk load: no replicas, no refresh.
await client.Indices.PutSettingsAsync("products_v2", s => s.Settings(x => x.NumberOfReplicas(0).RefreshInterval("-1")), ct);
// After the bulk load: restore both, then refresh.
await client.Indices.PutSettingsAsync("products_v2", s => s.Settings(x => x.NumberOfReplicas(1).RefreshInterval("1s")), ct);
await client.Indices.RefreshAsync("products_v2", ct);
```

The sync writes through the alias. During the load, changes go to the old index. After the alias move, run the outbox again from the start of the rebuild. Or write to both indexes during the rebuild (separate read and write aliases, SEARCH-03). External versions make a second write of the same change harmless.
