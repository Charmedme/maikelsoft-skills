using Microsoft.Extensions.Compliance.Classification;

namespace Orders.Api.Payments;

public static class PaymentTaxonomy
{
    public static DataClassification Personal => new("Payments", nameof(Personal));
}
