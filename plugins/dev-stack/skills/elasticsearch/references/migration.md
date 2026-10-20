# Migration from NEST and client 8

Read this file when the project uses NEST, Elasticsearch.Net, or Elastic.Clients.Elasticsearch 8.x.

## Contents

- Which client for which server
- Order of the upgrade
- NEST to client 9
- Client 8 to client 9

## Which client for which server

| Server | Client |
|--------|--------|
| 9.x | Elastic.Clients.Elasticsearch 9.x (9.5.3 on 2026-10-19) |
| 8.x | Elastic.Clients.Elasticsearch 8.19.x. Server 8.19 support ends on 2027-07-15. |
| 7.x | NEST 7.17.5 (deprecated). Plan the move to 8 and then 9. |

The client major must match the server major. The client does not follow semver: a minor version can change an API. Pin the exact version and read the release notes before each update.

## Order of the upgrade

1. Move the code to the client major of the current server. Example: NEST to client 8.19 on an 8.x server.
2. Upgrade the server.
3. Move the code to the client major of the new server.

Do one step per change, with the tests against a container of the target server version.

## NEST to client 9

NEST is a different library. The move is a rewrite of the calls, not a package update.

| NEST | Client 9 |
|------|----------|
| `using Nest;`, `using Elasticsearch.Net;` | `using Elastic.Clients.Elasticsearch;`, `using Elastic.Transport;` |
| `new ElasticClient(new ConnectionSettings(uri))` | `new ElasticsearchClient(new ElasticsearchClientSettings(uri))` |
| `IElasticClient` | `ElasticsearchClient`. It has no interface: use the in-memory transport in tests. |
| `response.IsValid` | `response.IsValidResponse` |
| `response.OriginalException`, `ServerError` | `response.DebugInformation`, `ElasticsearchServerError` |
| `new BasicAuthenticationCredentials(...)` in `ConnectionSettings` | `.Authentication(new ApiKey(key))` |
| `.Map<T>(m => m.AutoMap())` | An explicit mapping in `Mappings(m => m.Properties(...))` |
| Low-level calls through `client.LowLevel` | The typed request, or `client.Transport.RequestAsync` for an API that the client does not cover |

## Client 8 to client 9

| Client 8 code | Client 9 code |
|---------------|---------------|
| `s.Index("orders")` on a search | `s.Indices("orders")` (the old call is obsolete, CS0618) |
| `Query.MatchAll(new MatchAllQuery())` and other static factories | `new Query { MatchAll = new MatchAllQuery() }`, or the fluent `q => q.MatchAll(...)` |
| `query.TryGet<MatchQuery>(out var match)` | `query.Match is { } match` |
| `.ScaledFloat(x => x.Total)`, `.Number(x => x.Version)` | `.ScaledFloatNumber(x => x.Total, f => f.ScalingFactor(100))`, `.LongNumber(x => x.Version)` |
| `new FieldSort()` without a field | `new FieldSort { Field = ... }`, or the fluent `Sort(...)` |

A build with `-warnaserror` finds most of these: CS0618 for obsolete calls, CS1061 and CS0246 for removed members and types. Then run the tests against a 9.x container.

The full list of breaking changes is on the client page "Breaking changes" (`references/sources.md`).
