using Orders.Api.Orders;

namespace Orders.Api.Shipping.Carriers;

public sealed record CarrierBookingRequest(Guid ShipmentId, string CarrierCode, DeliveryAddress Address, string? RecipientEmail, string? Instructions);

public sealed record CarrierBooking(string TrackingNumber, Uri? LabelUrl);

public enum BookingOutcome
{
    Booked,
    Refused,
    Failed,
}

public sealed record CarrierBookingResult(BookingOutcome Outcome, CarrierBooking? Booking, string? ReasonCode, string? ReasonMessage)
{
    public static CarrierBookingResult Booked(CarrierBooking booking) => new(BookingOutcome.Booked, booking, null, null);

    public static CarrierBookingResult Refused(string code, string message) => new(BookingOutcome.Refused, null, code, message);

    public static CarrierBookingResult Failed() => new(BookingOutcome.Failed, null, null, null);
}
