namespace Orders.Api.Pricing;

public interface IPriceCalculator
{
    Task<decimal> CalculateTotalAsync(Guid customerId, IReadOnlyList<PriceLine> lines, CancellationToken ct);

    decimal CalculateTotal(Guid customerId, IReadOnlyList<PriceLine> lines);
}

public sealed record PriceLine(string Sku, int Quantity, decimal UnitPrice);
