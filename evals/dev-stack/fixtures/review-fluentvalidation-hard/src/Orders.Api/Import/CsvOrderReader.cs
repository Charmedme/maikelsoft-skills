using System.Globalization;
using Orders.Api.Orders;

namespace Orders.Api.Import;

// Reads the order export of the sales team: "externalRef;customerId;sku;quantity;unitPrice", one line per order line.
// Lines with the same externalRef form one order. The first line is the header.
public static class CsvOrderReader
{
    public const string RowInvalid = "import.row_invalid";

    public static (IReadOnlyList<ImportedOrder> Orders, IReadOnlyList<ImportRejection> Rejected) Read(string csv)
    {
        var groups = new Dictionary<string, (Guid? CustomerId, List<CreateOrderLineRequest> Lines)>(StringComparer.Ordinal);
        var rejected = new List<ImportRejection>();
        var rows = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1);
        var number = 1;
        foreach (var row in rows)
        {
            number++;
            var cells = row.Split(';');
            if (cells.Length != 5
                || !int.TryParse(cells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
                || !decimal.TryParse(cells[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var unitPrice))
            {
                rejected.Add(new ImportRejection($"row {number}", [RowInvalid]));
                continue;
            }

            var externalRef = cells[0].Trim();
            Guid? customerId = Guid.TryParse(cells[1], out var id) ? id : null;
            if (!groups.TryGetValue(externalRef, out var group))
            {
                group = (customerId, []);
                groups[externalRef] = group;
            }

            group.Lines.Add(new CreateOrderLineRequest(cells[2].Trim(), quantity, unitPrice));
        }

        var orders = groups.Select(g => new ImportedOrder(g.Key, new CreateOrderRequest(g.Value.CustomerId, g.Value.Lines))).ToList();
        return (orders, rejected);
    }
}
