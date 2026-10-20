# Sources

Checked on 2026-10-19. The facts of this skill come from these primary sources. Look them up again when `metadata.verified` in `SKILL.md` is older than 6 months.

## Contents

- Versions
- Documentation
- Checked by compile and test
- Credits

## Versions

| Item | Version on 2026-10-19 | Source |
|------|-----------------------|--------|
| Elastic.Clients.Elasticsearch | 9.5.3 (2026-09-28), needs Elastic.Transport 1.1.0 or later | [nuget.org](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) |
| Elastic.Clients.Elasticsearch 8.x | 8.19.27 | [nuget.org](https://www.nuget.org/packages/Elastic.Clients.Elasticsearch) |
| Elasticsearch server | 9.5.5, 9.4.8, 8.19.23 | [Docker Hub elasticsearch](https://hub.docker.com/_/elasticsearch/tags) |
| Testcontainers.Elasticsearch | 4.16.0 (default image 8.6.1) | [nuget.org](https://www.nuget.org/packages/Testcontainers.Elasticsearch) |
| NEST | 7.17.5, deprecated | [nuget.org](https://www.nuget.org/packages/NEST) |
| OpenSearch.Client | 2.2.0 | [nuget.org](https://www.nuget.org/packages/OpenSearch.Client) |
| OpenSearch server | 3.9.0, 2.19.6 | [Docker Hub opensearchproject/opensearch](https://hub.docker.com/r/opensearchproject/opensearch/tags) |

## Documentation

- [Elasticsearch .NET client: getting started and connecting](https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet): setup, API keys, TLS.
- [Elasticsearch .NET client: breaking changes](https://www.elastic.co/docs/release-notes/elasticsearch/clients/dotnet/breaking-changes): the 8.x to 9.x list.
- [Mapping](https://www.elastic.co/docs/manage-data/data-store/mapping): explicit mapping, `dynamic: strict`, field types.
- [Aliases](https://www.elastic.co/docs/manage-data/data-store/aliases): `is_write_index`, atomic alias actions.
- [Optimistic concurrency and versioning](https://www.elastic.co/docs/reference/elasticsearch/rest-apis/optimistic-concurrency-control): `version_type=external`.
- [Paginate search results](https://www.elastic.co/docs/reference/elasticsearch/rest-apis/paginate-search-results): `max_result_window`, `search_after`, point in time.
- [Query and filter context](https://www.elastic.co/docs/explore-analyze/query-filter/languages/querydsl): filters do not score and are cached.
- [Tune for indexing speed](https://www.elastic.co/docs/deploy-manage/production-guidance/optimize-performance/indexing-speed): replicas 0 and refresh `-1` for a bulk load, bulk size.
- [Language analyzers](https://www.elastic.co/docs/reference/text-analysis/analysis-lang-analyzer): `dutch`, `german`.
- [PostgreSQL: transaction ID wraparound](https://www.postgresql.org/docs/current/routine-vacuuming.html#VACUUM-FOR-WRAPAROUND): `xmin` is 32 bits.

## Checked by compile and test

On 2026-10-19 with the .NET SDK 10.0.402 and client 9.5.3:

- In-memory transport: an index call with a 409 gave no exception by default (`IsValidResponse == false`) and a `TransportException` with `ThrowExceptions()`. The request URL had `version_type=external&version=7`.
- In-memory transport: a bulk response with one 409 item gave `Errors == true` and `IsValidResponse == false` on a 200 response, and `ThrowExceptions()` did not throw.
- The eval probe (a fake HTTP server, the orders-api fixture): the client sent gzip-compressed request bodies by default. A worker that skips 409 items indexed the next order. A worker with a blanket throw logged the stale item as an error and retried the batch.
- `s.Index(...)` on a search gave CS0618. `CertificateValidations.AllowAll` and `(_, _, _, _) => true` both compiled with no warning.
- All C# examples in this skill compiled with warnings as errors. The container example in `testing.md` compiled but did not run: the test machine had no disk space for the Elasticsearch image.

## Credits

- elastic/agent-skills (Apache-2.0): `elasticsearch-index-design` and `elasticsearch-reindex`, paraphrased in `index-design.md`.
- elasticsearch-net documentation (Apache-2.0): connect, CRUD, and mapping syntax, paraphrased. Each example was compiled again.
