using System.Diagnostics.Metrics;

namespace Orders.Api.Orders;

public sealed class OrderMetrics
{
    private readonly Counter<long> _created;

    public OrderMetrics(IMeterFactory meterFactory)
    {
        _created = meterFactory.Create("Orders.Api").CreateCounter<long>("orders.created");
    }

    public void OrderCreated(Guid customerId) =>
        _created.Add(1, new KeyValuePair<string, object?>("customer.id", customerId));
}
