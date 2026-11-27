using System.Globalization;
using System.Xml;

namespace Kiosk.Api.B2B;

/// <summary>Reads the XML order export of retail partners.</summary>
public sealed class XmlOrderImporter
{
    public PartnerOrderDocument Parse(Stream xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        using var reader = XmlReader.Create(xml, settings);
        var document = new XmlDocument { XmlResolver = null };
        document.Load(reader);

        var root = document.DocumentElement ?? throw new FormatException("The document has no order element.");
        var order = new PartnerOrderDocument
        {
            Reference = root.GetAttribute("reference"),
            DeliveryAddress = root["deliveryAddress"]?.InnerText.Trim() ?? string.Empty,
            Note = root["note"]?.InnerText.Trim(),
        };
        foreach (XmlElement line in root.GetElementsByTagName("line"))
        {
            order.Lines.Add(new PartnerOrderLine
            {
                Sku = line.GetAttribute("sku"),
                Quantity = int.Parse(line.GetAttribute("quantity"), CultureInfo.InvariantCulture),
            });
        }

        return order;
    }
}
