# Client and errors

Setup of `ElasticsearchClient`, TLS and authentication, and how to handle responses and bulk results. Package `Elastic.Clients.Elasticsearch` 9.5.3 (needs `Elastic.Transport` 1.1.0 or later).

## Contents

- Setup
- TLS and authentication
- Response handling
- Bulk results
- Debugging

## Setup

Register one client for the process. It is thread-safe, pools connections, and caches metadata. A new client per call or per scope loses all of that.

```csharp
public static IServiceCollection AddSearch(this IServiceCollection services, Uri endpoint, string apiKey, X509Certificate2 ca)
{
    var settings = new ElasticsearchClientSettings(endpoint)
        .Authentication(new ApiKey(apiKey))
        .ServerCertificateValidationCallback(CertificateValidations.AuthorityIsRoot(ca))
        .DefaultMappingFor<ProductDocument>(m => m.IndexName("products"))
        .RequestTimeout(TimeSpan.FromSeconds(10));
    return services.AddSingleton(new ElasticsearchClient(settings));
}
```

- `DefaultMappingFor<T>` with the alias lets a call without an index name use the alias (SEARCH-03).
- `RequestTimeout`: the default is 1 minute. A search endpoint needs a shorter one.
- The client compresses request bodies with gzip by default. A fake server in a test must decompress them (`references/testing.md`).
- The default serializer writes property names in camelCase. A mapping with lambdas (`p.Keyword(d => d.Status)`) uses the same names.

## TLS and authentication

| Need | Setting |
|------|---------|
| Self-managed cluster with its own CA | `CertificateFingerprint(sha256)` or `ServerCertificateValidationCallback(CertificateValidations.AuthorityIsRoot(ca))` |
| Elastic Cloud | `new ElasticsearchClient(cloudId, new ApiKey(key))`: the public CA is trusted |
| Authentication | An API key with only the index privileges that the service needs. Not the `elastic` user. |

Never write `CertificateValidations.AllowAll` or a callback that returns `true`. Both compile without a warning, so only a review and stackcheck (ES-TLS-OFF) find them. Load the API key from a secret store (`dev-stack:secure-coding`).

## Response handling

The client does not throw by default. A failed call returns a response with `IsValidResponse == false`:

- `ElasticsearchServerError` holds the server error (type, reason, status), when the server answered.
- `ApiCallDetails.HttpStatusCode` holds the status, when there was a response.
- `DebugInformation` holds a readable summary for a log or an exception.

One helper keeps one style (SEARCH-05):

```csharp
internal static class ElasticsearchResponses
{
    // The client does not throw on an error response by default (SEARCH-05).
    public static T EnsureValid<T>(this T response)
        where T : ElasticsearchResponse =>
        response.IsValidResponse
            ? response
            : throw new InvalidOperationException($"Elasticsearch request failed: {response.DebugInformation}");
}
```

`ElasticsearchResponse` is in the namespace `Elastic.Transport.Products.Elasticsearch`.

When a status is expected, test it before the helper. Example: a single index call with an external version returns 409 for a stale write. That is a normal result for a sync.

```csharp
if (!response.IsValidResponse && response.ElasticsearchServerError?.Status != 409)
{
    throw new InvalidOperationException(response.DebugInformation);
}
```

`ThrowExceptions()` on the settings makes each failed call throw a `TransportException`. It does not throw for a failed item in a bulk response.

## Bulk results

A bulk response is HTTP 200 when the request itself worked. Each item has its own status. When one item fails, `Errors` is `true` and `IsValidResponse` is `false`, on the same 200 response. So:

1. Check the request: `response.ApiCallDetails.HttpStatusCode` is 200. Else the whole request failed (network, 429 on the request, 5xx).
2. Loop over `response.ItemsWithErrors`, and act on each status:

| Item status | Meaning | Action |
|-------------|---------|--------|
| 409 | The index has this version or a newer one (external version) | Skip. Log at debug level. |
| 429 | The cluster rejects work (queue full) | Retry the item later, with backoff |
| 5xx | Shard or node failure | Retry the item later |
| 400 | Bad document (for example `strict_dynamic_mapping_exception`) | Do not retry. Log and alert: the mapping or the document is wrong. |
| 404 | Delete of a document that does not exist | Skip |

Do not write `if (!bulk.IsValidResponse) throw`: it throws for a stale 409 item, the worker retries the whole batch, and the batch fails again each time.

Find the bulk size with a benchmark on one node with one shard. Start with 100 documents per request. Double the count until the indexing speed stops going up. When two sizes give the same speed, take the smaller one. Stay below a few tens of MB per request, also when larger requests look faster (Elastic, "Tune for indexing speed").

## Debugging

`DisableDirectStreaming()` keeps the request and response bytes in `ApiCallDetails`, so `DebugInformation` shows the bodies. It costs memory: use it in development and tests only. `OnRequestCompleted(details => ...)` sees each call, for example to record requests in a test.
