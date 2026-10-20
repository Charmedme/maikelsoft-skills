using Microsoft.Extensions.Compliance.Classification;

namespace Orders.Api.Telemetry;

public static class DataTaxonomy
{
    public static string TaxonomyName => "Orders";

    public static DataClassification Personal => new(TaxonomyName, nameof(Personal));
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PersonalDataAttribute : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    {
    }
}
