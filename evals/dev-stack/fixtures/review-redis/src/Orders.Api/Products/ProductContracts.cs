using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Products;

public sealed record ChangePriceRequest([property: Range(typeof(decimal), "0", "100000")] decimal Price);

public sealed record PriceResponse(string Sku, decimal Price);
