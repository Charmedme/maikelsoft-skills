using Wolverine.Attributes;

namespace Orders.Api.Contracts;

// Events of the carrier adapter. It translates the tracking webhooks of the carriers.

[MessageIdentity("carriers.parcel-delivered", Version = 1)]
public sealed record ParcelDelivered(string Carrier, string TrackingNumber, DateTimeOffset DeliveredAt);
