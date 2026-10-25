using Orders.Api.Orders;

namespace Orders.Api.Import;

public sealed record ImportedOrder(string ExternalRef, CreateOrderRequest Request);

public sealed record ImportRejection(string ExternalRef, IReadOnlyList<string> Codes);

public sealed record ImportReport(int Imported, IReadOnlyList<ImportRejection> Rejected);
