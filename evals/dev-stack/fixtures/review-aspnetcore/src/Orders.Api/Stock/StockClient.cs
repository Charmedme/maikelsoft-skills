using System.Net.Http.Json;

namespace Orders.Api.Stock;

public sealed class StockClient(IConfiguration configuration, ILogger<StockClient> logger)
{
    public async Task ReleaseAsync(Guid orderId, IEnumerable<StockLine> lines, CancellationToken ct)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(configuration["Stock:BaseUrl"]!);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await client.PostAsJsonAsync("/releases", new { orderId, lines }, ct);
                response.EnsureSuccessStatusCode();
                return;
            }
            catch (Exception ex) when (attempt < 3)
            {
                logger.LogWarning(ex, "Stock release attempt {Attempt} failed", attempt);
                await Task.Delay(TimeSpan.FromSeconds(attempt), ct);
            }
        }
    }
}

public sealed record StockLine(string Sku, int Quantity);
