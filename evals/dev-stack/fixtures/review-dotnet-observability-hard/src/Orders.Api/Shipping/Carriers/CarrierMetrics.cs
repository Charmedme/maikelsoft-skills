using System.Diagnostics.Metrics;

namespace Orders.Api.Shipping.Carriers;

// Booking metrics for the operations dashboard: the number of bookings and the booking time,
// per carrier and per outcome. The histogram count is the number of bookings.
public static class CarrierMetrics
{
    public const string MeterName = "Orders.Carriers";
    public const string BookingDuration = "orders.carrier.booking.duration";

    private static readonly Meter s_meter = new(MeterName);
    private static readonly Histogram<double> s_bookingDuration =
        s_meter.CreateHistogram<double>(BookingDuration, unit: "s", description: "Time of one booking with the carrier gateway.");

    // The postal code lets operations see the regions where a carrier refuses bookings.
    public static void BookingCompleted(string carrierCode, BookingOutcome outcome, string postalCode, double duration) =>
        s_bookingDuration.Record(
            duration,
            new KeyValuePair<string, object?>("orders.carrier", carrierCode),
            new KeyValuePair<string, object?>("orders.booking.outcome", Outcome(outcome)),
            new KeyValuePair<string, object?>("orders.postal_code", postalCode));

    private static string Outcome(BookingOutcome outcome) => outcome switch
    {
        BookingOutcome.Booked => "booked",
        BookingOutcome.Refused => "refused",
        _ => "failed",
    };
}
