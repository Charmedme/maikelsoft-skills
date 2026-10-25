using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Orders.Api.Shipping;

public static class CarrierCatalog
{
    private static readonly Dictionary<string, Carrier> s_carriers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["postnl"] = new("postnl", "PostNL", "https://jouw.postnl.nl/track-and-trace/{0}"),
        ["dhl"] = new("dhl", "DHL", "https://www.dhl.com/nl-en/home/tracking.html?tracking-id={0}"),
        ["dpd"] = new("dpd", "DPD", "https://tracking.dpd.de/parcelstatus?query={0}"),
    };

    public static IReadOnlyCollection<Carrier> All => s_carriers.Values;

    public static bool TryGet(string code, [NotNullWhen(true)] out Carrier? carrier) => s_carriers.TryGetValue(code, out carrier);

    public static Uri TrackingUrl(Carrier carrier, string trackingNumber) =>
        new(string.Format(CultureInfo.InvariantCulture, carrier.TrackingUrlTemplate, Uri.EscapeDataString(trackingNumber)));
}

public sealed record Carrier(string Code, string DisplayName, string TrackingUrlTemplate);
