namespace Orders.Api.Catalog;

public static class PriceCalculator
{
    public static decimal VatPercentage(VatRate rate) => rate switch
    {
        VatRate.Standard => 21m,
        VatRate.Reduced => 9m,
        VatRate.Zero => 0m,
        _ => throw new ArgumentOutOfRangeException(nameof(rate), rate, "Unknown VAT rate."),
    };

    public static decimal VatAmount(decimal netAmount, VatRate rate) =>
        Math.Round(netAmount * VatPercentage(rate) / 100m, 2, MidpointRounding.AwayFromZero);

    public static decimal Gross(decimal netAmount, VatRate rate) => netAmount + VatAmount(netAmount, rate);
}
