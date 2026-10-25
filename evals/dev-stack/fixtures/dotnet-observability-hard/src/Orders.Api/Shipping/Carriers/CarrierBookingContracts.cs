using Orders.Api.Orders;

namespace Orders.Api.Shipping.Carriers;

public sealed record CarrierBookingRequest(Guid ShipmentId, string CarrierCode, DeliveryAddress Address, string? RecipientEmail, string? Instructions);

public sealed record CarrierBooking(string TrackingNumber, Uri? LabelUrl);
