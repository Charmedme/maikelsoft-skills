namespace Orders.Api.Orders;

public interface IPostalCodeLookup
{
    Task<bool> ExistsAsync(string country, string postalCode, CancellationToken ct);
}

// In-memory lookup until the address service of the carrier is connected.
public sealed class InMemoryPostalCodeLookup : IPostalCodeLookup
{
    private static readonly HashSet<string> s_known = new(StringComparer.OrdinalIgnoreCase) { "7271AA", "7271AB", "1012AB" };

    public async Task<bool> ExistsAsync(string country, string postalCode, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(5), ct);
        return country == "NL" && s_known.Contains(postalCode.Replace(" ", "", StringComparison.Ordinal));
    }
}
