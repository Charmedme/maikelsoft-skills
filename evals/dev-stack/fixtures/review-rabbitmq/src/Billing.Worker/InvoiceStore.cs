using System.Collections.Concurrent;

namespace Billing.Worker;

/// <summary>Stands in for the invoice table. One invoice per order.</summary>
public sealed class InvoiceStore
{
    private readonly ConcurrentDictionary<Guid, decimal> _invoices = new();

    public int Count => _invoices.Count;

    public void CreateForOrder(OrderPlaced message)
    {
        if (message.Total < 0)
        {
            throw new ArgumentException("The total must not be negative.", nameof(message));
        }

        _invoices.TryAdd(message.OrderId, message.Total);
    }
}
