using Microsoft.Extensions.Options;

namespace Orders.Api.Pricing;

// The configuration binder skips keys and values that it cannot convert (Guid keys, unknown enum names)
// without an error. Bind strings, then parse and validate them on start.
public sealed class PricingOptions
{
    public const string SectionName = "Pricing";

    public Dictionary<string, string> CustomerTiers { get; set; } = [];

    public CustomerTier TierOf(Guid customerId)
    {
        foreach (var (key, value) in CustomerTiers)
        {
            if (Guid.TryParse(key, out var id) && id == customerId)
            {
                return Enum.Parse<CustomerTier>(value, ignoreCase: true);
            }
        }

        return CustomerTier.Bronze;
    }
}

public sealed class PricingOptionsValidator : IValidateOptions<PricingOptions>
{
    public ValidateOptionsResult Validate(string? name, PricingOptions options)
    {
        var errors = new List<string>();
        foreach (var (key, value) in options.CustomerTiers)
        {
            if (!Guid.TryParse(key, out _))
            {
                errors.Add($"Pricing:CustomerTiers: '{key}' is not a customer id (Guid).");
            }

            if (!Enum.TryParse<CustomerTier>(value, ignoreCase: true, out var tier) || !Enum.IsDefined(tier) || int.TryParse(value, out _))
            {
                errors.Add($"Pricing:CustomerTiers:{key}: '{value}' is not a tier (Bronze, Silver, Gold).");
            }
        }

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

public static class PricingRegistration
{
    public static IServiceCollection AddPricing(this IServiceCollection services)
    {
        services.AddSingleton<IValidateOptions<PricingOptions>, PricingOptionsValidator>();
        services.AddOptions<PricingOptions>()
            .BindConfiguration(PricingOptions.SectionName)
            .ValidateOnStart();
        return services;
    }
}
