using Elastic.Transport.Products.Elasticsearch;

namespace Orders.Api.Search;

internal static class ElasticsearchResponses
{
    // The client does not throw on an error response by default (SEARCH-05).
    public static T EnsureValid<T>(this T response)
        where T : ElasticsearchResponse =>
        response.IsValidResponse
            ? response
            : throw new InvalidOperationException($"Elasticsearch request failed: {response.DebugInformation}");
}
