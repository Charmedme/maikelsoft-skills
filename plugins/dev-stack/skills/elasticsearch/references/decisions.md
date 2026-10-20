# Search decisions

The team decisions that search code needs. The order is the order in which a new project meets them. The recommendation is the catalog default. Ask the human before you apply it (stack rule 4).

## Contents

- SEARCH-01. When to add a search engine
- SEARCH-04. Search server target
- SEARCH-02. Sync model from database to search index
- SEARCH-03. Index naming and aliases
- SEARCH-05. Client error handling
- SEARCH-06. Dutch and German text

## SEARCH-01. When to add a search engine

- Question: Does this feature need a search engine, or is the full-text search of the database enough?
- Ask when: the first search feature of a service.
- Options: Database full text: PostgreSQL `tsvector` with a GIN index, or a SQL Server full-text index. No second system, no sync. Elasticsearch: relevance tuning, typo tolerance, facets, large volumes, a second system with a sync.
- Recommended: Database full text first. Add Elasticsearch when a stated need is not met: relevance, typo tolerance, facets, or a volume that the database cannot serve.
- Choose another option when: the product already runs Elasticsearch and the feature fits its index model.
- Depends on: DATA-01.
- Revisit when: search queries load the primary database, or users ask for typo tolerance or facets.
- Check: judge.
- Detect: DET-SEARCH-01.

## SEARCH-04. Search server target

- Question: Which search server and client do we use?
- Ask when: the first Elasticsearch code, or an upgrade of the server.
- Options: Elasticsearch 9.5 with client 9.5.x: current major. Elasticsearch 8.19 with client 8.19.x: for an existing 8.x cluster (support ends 2027-07-15). OpenSearch with OpenSearch.Client: Apache-2.0, other client, other API in places.
- Recommended: Elasticsearch 9.5 with client 9.5.x, pinned to an exact version. The client is not backward compatible across majors and does not follow semver, so read the release notes before an update.
- Choose another option when: the license (ELv2, AGPL, SSPL) is not allowed, or the platform is AWS OpenSearch Service: OpenSearch (`references/opensearch.md`). The cluster is 8.x and cannot move: client 8.19.x.
- Depends on: SEARCH-01.
- Revisit when: a new server major, or the end of support of the current one.
- Check: tool (ES-PKG-NEST, ES-PKG-ESNET), judge.
- Detect: DET-SEARCH-04.

## SEARCH-02. Sync model from database to search index

- Question: How does a change in the database reach the search index?
- Ask when: the first index that holds data from the database.
- Options: Transactional outbox and an indexing worker: the outbox row is in the same transaction as the data. A second write in the request (dual write): simple, and the index is wrong when the second write fails. Change data capture (Debezium, logical replication): no code in the app, one more system. A periodic full rebuild: simple, the index is stale between runs.
- Recommended: Transactional outbox (the same mechanism as MSG-09) with an indexing worker. The external version is a `long Version` column that each save increments (the DATA-14 interceptor), or the outbox sequence number. Not `xmin`: it is 32 bits and wraps around, and then a newer update has a smaller number.
- Choose another option when: the index only needs daily data: a periodic rebuild. MSG-09 has no accepted answer and the service has no Wolverine: an outbox table plus a `BackgroundService` is enough for one indexer.
- Depends on: SEARCH-01, MSG-09, DATA-14.
- Revisit when: more than one consumer needs the same changes (then a broker), or the outbox grows faster than the worker reads it.
- Check: judge.
- Detect: DET-SEARCH-02.

## SEARCH-03. Index naming and aliases

- Question: How do we name indexes, and how does code address them?
- Ask when: the first index.
- Options: `{entity}_v{n}` physical, alias `{entity}` for read and write: a rebuild without downtime. The plain name only: a mapping change needs downtime. Separate read and write aliases: more control during a rebuild, more code.
- Recommended: `{entity}_v{n}` physical, alias `{entity}` for read and write (`is_write_index`). Code uses only the alias. A rebuild creates `_v{n+1}` and moves the alias with one `UpdateAliasesAsync` call.
- Choose another option when: the sync must keep writing during a long rebuild. Then separate read and write aliases, and write to both indexes during the rebuild.
- Depends on: SEARCH-04.
- Revisit when: a rebuild takes longer than the outbox can buffer.
- Check: judge.
- Detect: DET-SEARCH-03.

## SEARCH-05. Client error handling

- Question: How does code detect a failed Elasticsearch call?
- Ask when: the first Elasticsearch call.
- Options: No throw (the client default) plus one `EnsureValid()` helper that throws with `DebugInformation`. `ThrowExceptions()` on the client settings: each failed call throws a `TransportException`. Both need item-by-item handling of a bulk response, because a failed item does not throw.
- Recommended: No throw by default, one `EnsureValid()` helper, item-by-item handling for bulk responses. Bulk needs the item loop in any case, and the helper keeps one style for both.
- Choose another option when: the team prefers exceptions for all calls: `ThrowExceptions()`, and still loop over `ItemsWithErrors` after each bulk call.
- Depends on: SEARCH-04.
- Revisit when: the client changes its default.
- Check: judge.
- Detect: DET-SEARCH-05.

## SEARCH-06. Dutch and German text

- Question: How do we analyze Dutch and German text?
- Ask when: the first `text` field with Dutch or German content.
- Options: The built-in `dutch` and `german` analyzers on language sub-fields (`description.nl`, `description.de`). The `standard` analyzer: no stemming, no stop words. A custom analyzer (decompounder for German compound words, synonyms): better recall, more to maintain.
- Recommended: The built-in `dutch` and `german` analyzers on language sub-fields, decided per index, and checked with `_analyze` in a test.
- Choose another option when: users search German compound words (for example "Haustür" for "Tür"): a custom analyzer with a `dictionary_decompounder` or `hyphenation_decompounder`.
- Depends on: SEARCH-03.
- Revisit when: search logs show missed hits for compound words or synonyms.
- Check: judge (an `_analyze` test).
- Detect: DET-SEARCH-06.
