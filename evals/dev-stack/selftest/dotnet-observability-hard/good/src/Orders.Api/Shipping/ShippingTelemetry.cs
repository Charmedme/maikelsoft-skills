using System.Diagnostics;
using System.Diagnostics.Metrics;
using Orders.Api.Shipping.Carriers;

namespace Orders.Api.Shipping;

// Spans of the shipment steps. Application Insights shows them under the request.
public static class ShippingTelemetry
{
    public const string Name = "Orders.Shipping";

    public static readonly ActivitySource Source = new(Name);
}

// Metrics of the shipment steps, for the operations dashboard. Tags are low-cardinality only:
// no customer, order or shipment id (one time series per value, and personal data in metrics).
public sealed class ShippingMetrics
{
    private readonly Counter<long> _dispatched;
    private readonly Counter<long> _delivered;
    private readonly Histogram<double> _bookingDuration;

    public ShippingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(ShippingTelemetry.Name);
        _dispatched = meter.CreateCounter<long>("orders.shipments.dispatched", unit: "{shipment}");
        _delivered = meter.CreateCounter<long>("orders.shipments.delivered", unit: "{shipment}");
        _bookingDuration = meter.CreateHistogram<double>("orders.carrier.booking.duration", unit: "s", description: "Time of one booking with the carrier gateway.");
    }

    public void Dispatched(string carrierCode) =>
        _dispatched.Add(1, new KeyValuePair<string, object?>("orders.carrier", carrierCode));

    public void Delivered(string carrierCode) =>
        _delivered.Add(1, new KeyValuePair<string, object?>("orders.carrier", carrierCode));

    // The histogram count is the number of bookings.
    public void BookingCompleted(string carrierCode, BookingOutcome outcome, TimeSpan duration) =>
        _bookingDuration.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("orders.carrier", carrierCode),
            new KeyValuePair<string, object?>("orders.booking.outcome", outcome switch
            {
                BookingOutcome.Booked => "booked",
                BookingOutcome.Refused => "refused",
                _ => "failed",
            }));
}
