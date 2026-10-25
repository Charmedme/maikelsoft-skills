using FluentAssertions;
using Orders.Api.Catalog;

namespace Orders.Api.Tests;

[TestClass]
public sealed class PriceCalculatorTests
{
    [TestMethod]
    [DataRow(VatRate.Standard, "100.00", "21.00")]
    [DataRow(VatRate.Reduced, "10.00", "0.90")]
    [DataRow(VatRate.Zero, "55.55", "0")]
    [DataRow(VatRate.Standard, "0.05", "0.01")]
    public void VatAmount_RoundsAwayFromZero(VatRate rate, string net, string expected)
    {
        PriceCalculator.VatAmount(decimal.Parse(net, System.Globalization.CultureInfo.InvariantCulture), rate)
            .Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void Gross_AddsVat()
    {
        PriceCalculator.Gross(10m, VatRate.Standard).Should().Be(12.10m);
    }
}
