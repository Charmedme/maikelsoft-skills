using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Stock;

public sealed class StockOptions
{
    public const string SectionName = "Stock";

    [Required]
    public Uri? BaseUrl { get; set; }
}

public sealed record StockRelease(Guid OrderId);

public sealed class StockClient(HttpClient http)
{
    public async Task ReleaseAsync(Guid orderId, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("releases", new StockRelease(orderId), ct);
        response.EnsureSuccessStatusCode();
    }
}
