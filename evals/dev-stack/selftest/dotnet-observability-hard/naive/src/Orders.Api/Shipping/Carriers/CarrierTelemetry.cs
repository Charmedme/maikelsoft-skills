using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Orders.Api.Shipping.Carriers;

// Spans and metrics of the carrier bookings, for the operations dashboard.
public sealed class CarrierTelemetry
{
    public const string Name = "Orders.Carriers";

    public static readonly ActivitySource Source = new(Name);

    private readonly Histogram<double> _bookingDuration;

    public CarrierTelemetry(IMeterFactory meterFactory) =>
        _bookingDuration = meterFactory.Create(Name).CreateHistogram<double>("orders.carrier.booking.duration", unit: "ms");

    public void BookingCompleted(string carrierCode, BookingOutcome outcome, double milliseconds) =>
        _bookingDuration.Record(
            milliseconds,
            new KeyValuePair<string, object?>("carrier", carrierCode),
            new KeyValuePair<string, object?>("outcome", outcome.ToString().ToLowerInvariant()));
}
