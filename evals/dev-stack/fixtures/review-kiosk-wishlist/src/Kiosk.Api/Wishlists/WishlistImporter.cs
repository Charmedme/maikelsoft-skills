using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Kiosk.Api.Wishlists;

public sealed record ImportedItem(string Sku, int Quantity, string? Note);

/// <summary>Reads the gift registry export of other shops (XML).</summary>
public sealed class WishlistImporter
{
    public IReadOnlyList<ImportedItem> Parse(Stream xml)
    {
        using var reader = XmlReader.Create(xml);
        var document = XDocument.Load(reader);
        return document.Root?.Elements("item")
            .Select(e => new ImportedItem(
                (string?)e.Attribute("sku") ?? string.Empty,
                int.Parse((string?)e.Attribute("quantity") ?? "1", CultureInfo.InvariantCulture),
                (string?)e.Element("note")))
            .ToList() ?? [];
    }
}
