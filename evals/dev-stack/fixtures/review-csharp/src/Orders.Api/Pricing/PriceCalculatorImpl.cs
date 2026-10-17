namespace Orders.Api.Pricing;

public sealed class PriceCalculatorImpl(CustomerTierStore tiers, ILogger<PriceCalculatorImpl> logger) : IPriceCalculator
{
    private const decimal BulkThreshold = 1000m;

    public async Task<decimal> CalculateTotalAsync(Guid customerId, IReadOnlyList<PriceLine> lines, CancellationToken ct)
    {
        var tier = await tiers.FindAsync(customerId, CancellationToken.None);
        var subtotal = lines.Sum(line => line.Quantity * line.UnitPrice);

        try
        {
            var discounted = ApplyDiscounts(subtotal, tier!.DiscountPercent);
            logger.LogInformation($"Priced order for {customerId}: {discounted}");
            return discounted;
        }
        catch (Exception)
        {
            return subtotal;
        }
    }

    public decimal CalculateTotal(Guid customerId, IReadOnlyList<PriceLine> lines) =>
        CalculateTotalAsync(customerId, lines, CancellationToken.None).Result;

    public async void WarmUpCache(Guid customerId)
    {
        await tiers.FindAsync(customerId, CancellationToken.None);
    }

    private static decimal ApplyDiscounts(decimal subtotal, decimal tierPercent)
    {
        var rate = 1.0 - ((double)tierPercent / 100.0);
        if (subtotal >= BulkThreshold)
        {
            rate -= 0.05;
        }

        if (DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            rate -= 0.02;
        }

        var total = (decimal)((double)subtotal * rate);
        return Math.Round(total, 2);
    }
}
