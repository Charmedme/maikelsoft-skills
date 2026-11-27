using Newtonsoft.Json;

namespace Kiosk.Api.B2B;

/// <summary>Reads the JSON order feed of partners that use the partner SDK.</summary>
public sealed class JsonOrderImporter
{
    // The SDK writes type names for its extension objects. Keeping them makes the archive copy round-trip.
    private static readonly JsonSerializerSettings s_settings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    public PartnerOrderDocument Parse(string json) =>
        JsonConvert.DeserializeObject<PartnerOrderDocument>(json, s_settings)
            ?? throw new FormatException("The document is empty.");
}
