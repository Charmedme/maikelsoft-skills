namespace Orders.Api.Pricing;

public sealed record CustomerTier(Guid CustomerId, string Name, decimal DiscountPercent);

public sealed class CustomerTierStore
{
    private readonly List<CustomerTier> _tiers = [];

    public void Add(CustomerTier tier) => _tiers.Add(tier);

    public Task<CustomerTier?> FindAsync(Guid customerId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_tiers.FirstOrDefault(tier => tier.CustomerId == customerId));
    }
}
