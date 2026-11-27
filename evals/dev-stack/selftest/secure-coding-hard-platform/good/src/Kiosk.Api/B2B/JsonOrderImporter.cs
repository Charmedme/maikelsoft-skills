using Newtonsoft.Json;

namespace Kiosk.Api.B2B;

/// <summary>Reads the JSON order feed of partners that use the partner SDK.</summary>
public sealed class JsonOrderImporter
{
    // Partner data never chooses a .NET type: extension objects stay plain JSON.
    private static readonly JsonSerializerSettings s_settings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        MissingMemberHandling = MissingMemberHandling.Ignore,
    };

    public PartnerOrderDocument Parse(string json) =>
        JsonConvert.DeserializeObject<PartnerOrderDocument>(json, s_settings)
            ?? throw new FormatException("The document is empty.");
}
