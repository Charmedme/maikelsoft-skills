using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Kiosk.Api.B2B;

/// <summary>Reads the price list that suppliers upload each month.</summary>
public sealed class PriceListImporter
{
    public IReadOnlyList<PriceUpdate> Parse(Stream xml)
    {
        using var reader = XmlReader.Create(xml);
        var document = XDocument.Load(reader);
        return document.Root?.Elements("price")
            .Select(e => new PriceUpdate(
                (string?)e.Attribute("sku") ?? string.Empty,
                decimal.Parse((string?)e.Attribute("amount") ?? "0", CultureInfo.InvariantCulture)))
            .ToList() ?? [];
    }
}
