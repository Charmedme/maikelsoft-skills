using System.ComponentModel.DataAnnotations;

namespace Orders.Api.Stock;

public sealed record PutStockRequest([property: Range(0, 1_000_000)] int Available);
