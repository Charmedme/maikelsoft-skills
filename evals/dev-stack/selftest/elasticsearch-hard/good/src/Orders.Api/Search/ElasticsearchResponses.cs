using Elastic.Transport.Products.Elasticsearch;

namespace Orders.Api.Search;

internal static class ElasticsearchResponses
{
    // The client does not throw on an error response by default.
    public static T EnsureValid<T>(this T response)
        where T : ElasticsearchResponse =>
        response.IsValidResponse
            ? response
            : throw new InvalidOperationException($"Elasticsearch request failed: {response.DebugInformation}");
}
