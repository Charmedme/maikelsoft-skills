using System.Globalization;

namespace Orders.Api.Invoices;

public static class InvoiceNumber
{
    // INV-2026-1A2B3C4D: the year of the order and the first 8 hex digits of the order id.
    public static string For(Guid orderId, DateTimeOffset placedAt) =>
        string.Create(CultureInfo.InvariantCulture, $"INV-{placedAt.Year}-{orderId.ToString("N")[..8].ToUpperInvariant()}");
}
