using Wolverine.Attributes;

namespace Orders.Api.Contracts;

// Contracts of the warehouse service. The warehouse team owns this file (see docs/warehouse.md).

[MessageIdentity("warehouse.pick-order", Version = 1)]
public sealed record PickOrder(
    Guid OrderId,
    Guid ShipmentId,
    string Carrier,
    string TrackingNumber,
    IReadOnlyList<PickLine> Lines);

public sealed record PickLine(string Sku, int Quantity);
