using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Products;

public sealed record CreateAlertRequest([property: Required] Guid? CustomerId);

public sealed record AlertResponse(string Sku, Guid CustomerId, DateTimeOffset CreatedAt, bool Notified);
