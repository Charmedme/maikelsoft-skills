using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Shipping;

// Spans of the shipment steps. Application Insights shows them under the request.
public static class ShippingTelemetry
{
    public const string Name = "Orders.Shipping";

    public static readonly ActivitySource Source = new(Name);
}

// Metrics of the shipment steps, for the operations dashboard.
public sealed class ShippingMetrics
{
    private readonly Counter<long> _dispatched;
    private readonly Counter<long> _delivered;

    public ShippingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ShippingTelemetry.Name);
        _dispatched = meter.CreateCounter<long>("orders.shipments.dispatched", unit: "{shipment}");
        _delivered = meter.CreateCounter<long>("orders.shipments.delivered", unit: "{shipment}");
    }

    // The key-account team filters the dispatches per customer.
    public void Dispatched(string carrierCode, Guid customerId) =>
        _dispatched.Add(
            1,
            new KeyValuePair<string, object?>("orders.carrier", carrierCode),
            new KeyValuePair<string, object?>("orders.customer_id", customerId));

    public void Delivered(string carrierCode) =>
        _delivered.Add(1, new KeyValuePair<string, object?>("orders.carrier", carrierCode));
}
