using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Orders.Api.Catalog;
using Orders.Api.Search;

namespace Orders.Api.Tests;

// Helpers for the search tests. The index worker runs in the background, so a search waits until it sees the change.
public static class SearchTestHelpers
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(20);

    public static string UniqueSku(string prefix) => $"{prefix}-{Guid.CreateVersion7():N}"[..30].ToUpperInvariant();

    public static async Task<ProductResponse> CreateProductAsync(
        this HttpClient client,
        string sku,
        string name,
        CancellationToken ct,
        string brand = "Example",
        string category = "general",
        decimal price = 10m)
    {
        var response = await client.PostAsJsonAsync("/products", new CreateProductRequest(sku, name, price, VatRate.Standard, brand, category), ct);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ProductResponse>(ct))!;
    }

    public static async Task<ProductSearchResponse> SearchUntilAsync(
        this HttpClient client,
        string query,
        Func<ProductSearchResponse, bool> done,
        CancellationToken ct)
    {
        var end = DateTime.UtcNow + s_timeout;
        while (true)
        {
            var result = (await client.GetFromJsonAsync<ProductSearchResponse>("/products/search?" + query, ct))!;
            if (done(result) || DateTime.UtcNow > end)
            {
                return result;
            }

            await Task.Delay(200, ct);
        }
    }
}
