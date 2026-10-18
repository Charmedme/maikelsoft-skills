namespace Orders.Api.Pricing;

public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    public Dictionary<Guid, CustomerTier> CustomerTiers { get; set; } = [];

    public CustomerTier TierOf(Guid customerId) =>
        CustomerTiers.GetValueOrDefault(customerId, CustomerTier.Bronze);
}

public static class PricingRegistration
{
    public static IServiceCollection AddPricing(this IServiceCollection services)
    {
        services.AddOptions<PricingOptions>()
            .BindConfiguration(PricingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
