# Review checklist

Each check is `tool (<rule ID>)` when a tool finds it, or `judge` when a reviewer must decide. Run the tools first (`SKILL.md`, section Check). In a review, cite the rule number of `SKILL.md` or the rule ID.

## Contents

- Packages and client
- Responses
- Index design
- Sync
- Queries
- Tests
- Decisions

## Packages and client

| # | Check | Type |
|---|-------|------|
| 1 | No NEST and no Elasticsearch.Net. | tool (ES-USING-NEST, ES-PKG-NEST, ES-PKG-ESNET) |
| 2 | The client major matches the server major, with an exact version. A 9.x client on an 8.x server is a defect. An 8.x client on a 9.x server works (forward compatible), but needs a planned upgrade. | judge |
| 3 | One `ElasticsearchClient` for the process. No `new ElasticsearchClient` in a method, a constructor, or a scoped service. | tool (ES-NEW-CLIENT), judge |
| 4 | Authentication with an API key with only the privileges that the service needs. The key comes from a secret store. | judge |
| 5 | TLS validation is on: `CertificateFingerprint` or `AuthorityIsRoot`, never `AllowAll` or a callback that returns `true`. | tool (ES-TLS-OFF) |
| 6 | `DefaultMappingFor<T>` uses the alias, and a `RequestTimeout` is set. | judge |

## Responses

| # | Check | Type |
|---|-------|------|
| 7 | Each response is checked: `IsValidResponse`, an `EnsureValid()` helper, or `ThrowExceptions()`. A `try/catch` alone is not a check. | judge |
| 8 | After each bulk call, the code reads `ItemsWithErrors`: it skips 409, retries 429 and 5xx, and reports 400. | judge |
| 9 | No blanket `if (!bulk.IsValidResponse) throw` in a sync with external versions. | judge |
| 10 | `response.Documents` and `Hits` are used only after the response is checked. | judge |

## Index design

| # | Check | Type |
|---|-------|------|
| 11 | Each index has an explicit mapping with `Dynamic(DynamicMapping.Strict)`. | judge |
| 12 | IDs, codes, and filter fields are `keyword`. Sorted text has a `keyword` sub-field with `IgnoreAbove`. Money is `scaled_float` or `long`. | judge |
| 13 | The physical index is `{entity}_v{n}`. Code uses only the alias. The alias moves with one `UpdateAliasesAsync` call, with one descriptor (lambda) for each action. `act.Remove(...).Add(...)` sends only the add. | judge |
| 14 | A full rebuild sets 0 replicas and refresh `-1`, and restores both before the alias moves. | judge |
| 15 | Dutch and German text uses the `dutch` and `german` analyzers on sub-fields (SEARCH-06), with an `_analyze` test. | judge |

## Sync

| # | Check | Type |
|---|-------|------|
| 16 | The request writes only to the database, with an outbox row in the same transaction. No index call in the request. | judge |
| 17 | Writes and deletes use `VersionType.External` with a monotonic 64-bit version. Not `xmin`, not a timestamp. | judge |
| 18 | The worker reads the current state, deletes only the processed outbox rows, and survives an exception. | judge |
| 19 | No `Refresh.True` in production code. `Refresh.WaitFor` only for read-your-write, never in a loop. | tool (ES-REFRESH-TRUE) |

## Queries

| # | Check | Type |
|---|-------|------|
| 20 | Non-scoring clauses (status, tenant, dates, permissions) are in `bool.filter`. | judge |
| 21 | `term` queries target `keyword` fields. | judge |
| 22 | User text goes through `match` or `simple_query_string`. No `query_string` on raw input, no query JSON built from strings. | tool (ES-QUERY-STRING), judge |
| 23 | `from + size` stays at or below 10,000. No `max_result_window` change. | tool (ES-MAX-RESULT-WINDOW), judge |
| 24 | An export uses a point in time with `search_after`, closes the point in time in `finally`, and streams the result. | judge |
| 25 | Paged sorts end with a unique tie-breaker. | judge |

## Tests

| # | Check | Type |
|---|-------|------|
| 26 | Mapping and query tests run against a container with the production server version, not the default image of the module. | judge |
| 27 | Response handling (409, failed bulk items) has tests with the in-memory transport. | judge |
| 28 | Tests refresh the index before they search. | judge |

## Decisions

| # | Check | Type |
|---|-------|------|
| 29 | New search code follows the accepted SEARCH decisions, or the output marks them as assumed. | judge |
