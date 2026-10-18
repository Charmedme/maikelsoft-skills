namespace Orders.Api.Pricing;

public enum CustomerTier
{
    Bronze,
    Silver,
    Gold,
}

public static class TierRates
{
    public static decimal Of(CustomerTier tier) => tier switch
    {
        CustomerTier.Gold => 0.10m,
        CustomerTier.Silver => 0.05m,
        _ => 0m,
    };
}
