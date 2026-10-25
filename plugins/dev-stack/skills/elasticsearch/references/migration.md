# Migration from NEST and client 8

Read this file when the project uses NEST, Elasticsearch.Net, or Elastic.Clients.Elasticsearch 8.x.

## Contents

- Which client for which server
- Order of the upgrade
- NEST to client 9
- Client 8.18 or older to client 8.19 and 9

## Which client for which server

| Server | Client |
|--------|--------|
| 9.x | Elastic.Clients.Elasticsearch 9.x (9.5.3 on 2026-10-25) |
| 8.x | Elastic.Clients.Elasticsearch 8.19.x (8.19.27 on 2026-10-25). Server 8.19 support ends on 2027-07-15. |
| 7.x | NEST 7.17.5 (deprecated). Plan the move to 8 and then 9. |

Use the client major of the server major. The clients are forward compatible: a client works with the same server major and the next higher major, but never with an earlier major. So an 8.x client works with a 9.x server (without the new features), and a 9.x client does not work with an 8.x server. NEST 7.17 does not work with a 9.x server.

The client does not follow semver: a minor or a patch version can change an API. Pin the exact version and read the release notes before each update.

## Order of the upgrade

1. Move the code to the client major of the current server. Example: NEST to client 8.19 on an 8.x server.
2. Upgrade the server. Client 8.19 keeps working against it.
3. Move the code to the client major of the new server. Never do this step before step 2.

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
| `.Number(n => n.Name(x => x.Total).Type(NumberType.ScaledFloat).ScalingFactor(100))` | `.ScaledFloatNumber(x => x.Total, f => f.ScalingFactor(100))` |
| `.Number(n => n.Name(x => x.Version).Type(NumberType.Long))` | `.LongNumber(x => x.Version)` |
| Low-level calls through `client.LowLevel` | The typed request, or `client.Transport.RequestAsync` for an API that the client does not cover |

## Client 8.18 or older to client 8.19 and 9

Client 8.19 has the API shape of client 9. From 8.19 to 9.x, the code changes are small. Change the package version, build, and read the breaking changes of each 9.x version that you pass. The table is for code on client 8.18 or older. Move it to 8.19 first, on the 8.x server.

| Client 8.18 or older | Client 8.19 and 9 |
|----------------------|-------------------|
| `s.Index("orders")` on a search | `s.Indices("orders")` (the old call is obsolete from 8.19, CS0618) |
| `Query.MatchAll(new MatchAllQuery())` and other static factories | `new Query { MatchAll = new MatchAllQuery() }`, or the fluent `q => q.MatchAll(...)` |
| `query.TryGet<MatchQuery>(out var match)` | `query.Match is { } match` |
| `new FieldSort()` without a field | `new FieldSort { Field = ... }`, or the fluent `Sort(...)` |

Client 8.18 also marks the synchronous methods obsolete (CS0618): use the `...Async` methods. A build with `-warnaserror` finds most of these: CS0618 for obsolete calls, CS1061, CS1955, and CS9035 for removed or changed members. Then run the tests against a container of the server version.

The full list of breaking changes is on the client page "Breaking changes" (`references/sources.md`).
