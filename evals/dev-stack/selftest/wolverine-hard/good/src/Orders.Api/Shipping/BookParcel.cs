using Orders.Api.Contracts;

namespace Orders.Api.Shipping;

// Local command: book the parcel of a shipment with the parcel platform. A separate step, so that a retry
// uses the same shipment id as the idempotency key.
public sealed record BookParcel(Guid ShipmentId, ShippingSpeed Speed);
