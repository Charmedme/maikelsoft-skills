# Testing search code

Which test for which behavior: a real container for mappings and queries, the in-memory transport for response handling. Test framework rules are in `dev-stack:dotnet-testing`.

## Contents

- Container
- In-memory transport
- A fake server for a whole app
- What a test must prove

## Container

Test mappings, analyzers, and queries against the production server version. Pass the image to the builder: the default image of `Testcontainers.Elasticsearch` 4.16.0 is 8.6.1.

```csharp
var container = new ElasticsearchBuilder("elasticsearch:9.5.5")
    .WithEnvironment("cluster.routing.allocation.disk.threshold_enabled", "false")
    .Build();
await container.StartAsync(ct);

// Trust the CA of this container only. TLS validation stays on.
var ca = X509CertificateLoader.LoadCertificate(
    await container.ReadFileAsync("/usr/share/elasticsearch/config/certs/http_ca.crt", ct));
var settings = new ElasticsearchClientSettings(new Uri(container.GetConnectionString()))
    .ServerCertificateValidationCallback(CertificateValidations.AuthorityIsRoot(ca));
var client = new ElasticsearchClient(settings);
```

- `disk.threshold_enabled=false`: a CI machine with a full disk otherwise makes the shards read-only, and the tests fail with no clear reason.
- The module gives the JVM 2 GB of heap. Start one container per test assembly.
- Call `client.Indices.RefreshAsync(alias)` after a write and before a search.
- Use a new index name per test class, or delete the documents of the test. Shared documents make tests depend on each other.

The code above compiled. It did not run on 2026-10-19: the test machine had no disk space for the Elasticsearch image.

## In-memory transport

Response handling (a 409, a bulk response with one failed item, the request URL and body) needs no server. `InMemoryRequestInvoker` returns a fixed response:

```csharp
var requests = new List<string>();
var settings = new ElasticsearchClientSettings(
        new SingleNodePool(new Uri("http://localhost:9200")),
        new InMemoryRequestInvoker(Encoding.UTF8.GetBytes(body), status, null, "application/json", new() { ["x-elastic-product"] = ["Elasticsearch"] }))
    .DisableDirectStreaming()
    .OnRequestCompleted(call => requests.Add(Encoding.UTF8.GetString(call.RequestBodyInBytes ?? [])));
var client = new ElasticsearchClient(settings);
```

- The header key must be lowercase `x-elastic-product` with the value `Elasticsearch`, and the content type `application/json`. Else the product check fails, also on a 2xx response.
- `DisableDirectStreaming` keeps the request body, so a test can check `"version_type":"external"`.

Example test:

```csharp
[TestMethod]
public async Task IndexAsync_OneStaleItem_SkipsItAndDoesNotThrow()
{
    var (index, _) = Create(200, BulkBody((409, "version_conflict_engine_exception"), (201, null)));

    var act = () => index.IndexAsync([s_order, s_order with { Id = "2" }], TestContext.CancellationToken);

    await act.Should().NotThrowAsync();
}
```

## A fake server for a whole app

To test the app with its worker through HTTP, point `ConnectionStrings:Search` at a small HTTP server in the test (`HttpListener`) that answers like Elasticsearch:

- Send the header `X-Elastic-Product: Elasticsearch` and `Content-Type: application/json` on each response.
- Decompress the request body when `Content-Encoding` is `gzip`. Client 9 compresses request bodies by default.
- Answer `HEAD /{index}` with 404 (the index does not exist yet), `PUT /{index}` with `{"acknowledged":true}`, and `POST /_bulk` with one item per action.

Use this for behavior across the app (the outbox reaches the index, a stale item does not stop the worker). Use the container for mappings and queries.

## What a test must prove

| Behavior | Test |
|----------|------|
| A stale (409) bulk item is skipped, other items index | In-memory transport with a bulk body that has one 409 item |
| A 5xx item fails the batch, so it runs again | In-memory transport with a 503 item |
| Writes carry the external version | Request body or URL has `version_type=external` and the version |
| The mapping is strict and types are correct | Container: index a document with an unknown field and expect 400 |
| The analyzer stems the language | Container: `_analyze` with `dutch` or `german` |
| Filters and sort give the expected hits | Container: index, refresh, search |
| The export returns all hits | Container: more hits than one page, count the CSV rows |
